using BugTrackr.Application.Common;
using BugTrackr.Application.Services;
using BugTrackr.Application.Services.Chat;
using BugTrackr.Domain.Entities;
using BugTrackr.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Chat;

public record DeleteMessageCommand(int MessageId, int UserId, int roomId) : IRequest<ApiResponse<string>>;

public class DeleteMessageCommandValidator : AbstractValidator<DeleteMessageCommand>
{
    public DeleteMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.roomId).GreaterThan(0);
    }
}

public class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand, ApiResponse<string>>
{
    private readonly IRepository<ChatMessage> _messageRepository;
    private readonly IRepository<ChatParticipant> _participantRepository;
    private readonly IValidator<DeleteMessageCommand> _validator;
    private readonly IChatNotificationService _notificationService;
    private readonly ILogger<DeleteMessageCommandHandler> _logger;

    public DeleteMessageCommandHandler(
        IRepository<ChatMessage> messageRepository,
        IRepository<ChatParticipant> participantRepository,
        IValidator<DeleteMessageCommand> validator,
        IChatNotificationService notificationService,
        ILogger<DeleteMessageCommandHandler> logger)
    {
        _messageRepository = messageRepository;
        _participantRepository = participantRepository;
        _notificationService = notificationService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(err => err.ErrorMessage);
                var resp = ApiResponse<string>.Failure("Validation failed", 400);
                resp.Errors = errors;
                return resp;
            }

            var message = await _messageRepository.GetByIdAsync(request.MessageId);
            if (message == null)
            {
                return ApiResponse<string>.Failure("Message not found", 404);
            }

            // Check if user can delete - either message sender or room admin/moderator
            bool canDelete = message.SenderId == request.UserId;

            if (!canDelete)
            {
                var participant = await _participantRepository.Query()
                    .FirstOrDefaultAsync(p => p.RoomId == message.RoomId && p.UserId == request.UserId, cancellationToken);

                canDelete = participant?.Role == ChatParticipantRole.Admin || participant?.Role == ChatParticipantRole.Moderator;
            }

            if (!canDelete)
            {
                return ApiResponse<string>.Failure("You don't have permission to delete this message", 403);
            }

            _messageRepository.Delete(message);
            await _messageRepository.SaveChangesAsync(cancellationToken);

            await _notificationService.NotifyMessageDeleted(request.roomId, request.MessageId);

            _logger.LogInformation("Message {MessageId} deleted by user {UserId}", request.MessageId, request.UserId);
            return ApiResponse<string>.SuccessResponse("Message deleted successfully", 200);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId}", request.MessageId);
            return ApiResponse<string>.Failure("An error occurred while deleting the message", 500);
        }
    }
}