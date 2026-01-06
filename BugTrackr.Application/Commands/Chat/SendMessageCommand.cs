using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.Chat;
using BugTrackr.Application.Services;
using BugTrackr.Application.Services.Chat;
using BugTrackr.Domain.Entities;
using BugTrackr.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace BugTrackr.Application.Commands.Chat;

public record SendMessageCommand(int RoomId, SendMessageDto MessageData, int SenderId) : IRequest<ApiResponse<ChatMessageDto>>;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.SenderId).GreaterThan(0);

        RuleFor(x => x.MessageData.Content)
            .NotEmpty()
            .MaximumLength(2000)
            .WithMessage("Message content cannot exceed 2000 characters");

        RuleFor(x => x.MessageData.Type)
            .NotEmpty()
            .Must(BeValidMessageType)
            .WithMessage("Invalid message type");
    }

    private bool BeValidMessageType(string type)
    {
        return Enum.TryParse<ChatMessageType>(type, true, out _);
    }
}


public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ApiResponse<ChatMessageDto>>
{
    private readonly IRepository<ChatMessage> _messageRepository;
    private readonly IRepository<ChatParticipant> _participantRepository;
    private readonly IRepository<MessageStatus> _statusRepository;
    private readonly IChatNotificationService _notificationService;
    private readonly IValidator<SendMessageCommand> _validator;
    private readonly ILogger<SendMessageCommandHandler> _logger;
    private readonly IMapper _mapper;

    public SendMessageCommandHandler(
        IRepository<ChatMessage> messageRepository,
        IRepository<ChatParticipant> participantRepository,
        IRepository<MessageStatus> statusRepository,
        IChatNotificationService notificationService,
        ILogger<SendMessageCommandHandler> logger,
        IValidator<SendMessageCommand> validator,
        IMapper mapper)
    {
        _messageRepository = messageRepository;
        _participantRepository = participantRepository;
        _statusRepository = statusRepository;
        _notificationService = notificationService;
        _validator = validator;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ChatMessageDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
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


            // Verify user is participant of the room
            var isParticipant = await _participantRepository.Query()
                .AnyAsync(p => p.RoomId == request.RoomId && p.UserId == request.SenderId, cancellationToken);

            if (!isParticipant)
            {
                return ApiResponse<ChatMessageDto>.Failure("You are not a participant of this chat room", 403);
            }

            // Create message
            var message = new ChatMessage
            {
                RoomId = request.RoomId,
                SenderId = request.SenderId,
                Content = request.MessageData.Content,
                Type = Enum.Parse<ChatMessageType>(request.MessageData.Type, true),
                ReplyToId = request.MessageData.ReplyToId,
                IsEdited = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _messageRepository.AddAsync(message);
            await _messageRepository.SaveChangesAsync(cancellationToken);

            // Create message statuses for all participants
            var participants = await _participantRepository.Query()
                .Where(p => p.RoomId == request.RoomId)
                .Select(p => p.UserId)
                .ToListAsync(cancellationToken);

            var messageStatuses = participants.Select(userId => new MessageStatus
            {
                MessageId = message.Id,
                UserId = userId,
                Status = userId == request.SenderId ? MessageStatusType.Read: MessageStatusType.Sent,
                Timestamp = DateTime.UtcNow
            }).ToList();

            await _statusRepository.AddRangeAsync(messageStatuses);
            await _statusRepository.SaveChangesAsync(cancellationToken);

            // Load message with sender info
            var createdMessage = await _messageRepository.Query()
                .Include(m => m.Sender)
                .Include(m => m.ReplyToMessage)
                .ThenInclude(rm => rm!.Sender)
                .Include(m => m.MessageStatuses)
                .FirstOrDefaultAsync(m => m.Id == message.Id, cancellationToken);

            var messageDto = _mapper.Map<ChatMessageDto>(createdMessage);

            // REAL-TIME NOTIFICATION
            await _notificationService.NotifyNewMessage(request.RoomId, messageDto, cancellationToken);

            return ApiResponse<ChatMessageDto>.SuccessResponse(messageDto, 201, "Message sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return ApiResponse<ChatMessageDto>.Failure("An error occurred while sending the message", 500);
        }
    }
}