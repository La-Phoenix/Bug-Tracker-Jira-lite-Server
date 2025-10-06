using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.Chat;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Chat;

public record EditMessageCommand(int MessageId, string Content, int UserId) : IRequest<ApiResponse<ChatMessageDto>>;

public class EditMessageCommandValidator : AbstractValidator<EditMessageCommand>
{
    public EditMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(2000)
            .WithMessage("Message content cannot exceed 2000 characters");
    }
}

public class EditMessageCommandHandler : IRequestHandler<EditMessageCommand, ApiResponse<ChatMessageDto>>
{
    private readonly IRepository<ChatMessage> _messageRepository;
    private readonly IValidator<EditMessageCommand> _validator;
    private readonly ILogger<EditMessageCommandHandler> _logger;
    private readonly IChatNotificationService _notificationService;
    private readonly IMapper _mapper;

    public EditMessageCommandHandler(
        IRepository<ChatMessage> messageRepository,
        IValidator<EditMessageCommand> validator,
        IChatNotificationService notificationService,
        IMapper mapper,
        ILogger<EditMessageCommandHandler> logger)
    {
        _messageRepository = messageRepository;
        _validator = validator;
        _notificationService = notificationService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<ChatMessageDto>> Handle(EditMessageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(err => err.ErrorMessage);
                var resp = ApiResponse<ChatMessageDto>.Failure("Validation failed", 400);
                resp.Errors = errors;
                return resp;
            }

            var message = await _messageRepository.Query()
                .Include(m => m.Sender)
                .Include(m => m.ReplyToMessage)
                .ThenInclude(rm => rm!.Sender)
                .Include(m => m.MessageStatuses)
                .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken);

            if (message == null)
            {
                return ApiResponse<ChatMessageDto>.Failure("Message not found", 404);
            }

            if (message.SenderId != request.UserId)
            {
                return ApiResponse<ChatMessageDto>.Failure("You can only edit your own messages", 403);
            }

            // Check if message is too old to edit (e.g., 24 hours)
            if (DateTime.UtcNow - message.CreatedAt > TimeSpan.FromHours(24))
            {
                return ApiResponse<ChatMessageDto>.Failure("Message is too old to edit", 400);
            }

            message.Content = request.Content;
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;

            _messageRepository.Update(message);
            await _messageRepository.SaveChangesAsync(cancellationToken);

            var messageDto = _mapper.Map<ChatMessageDto>(message);

            // 🚀 NOTIFY REAL-TIME MESSAGE EDIT
            await _notificationService.NotifyMessageEdited(message.RoomId, request.MessageId, request.Content);

            _logger.LogInformation("Message {MessageId} edited by user {UserId}", request.MessageId, request.UserId);
            return ApiResponse<ChatMessageDto>.SuccessResponse(messageDto, 200, "Message edited successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing message {MessageId}", request.MessageId);
            return ApiResponse<ChatMessageDto>.Failure("An error occurred while editing the message", 500);
        }
    }
}