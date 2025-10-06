using BugTrackr.Application.Common;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using BugTrackr.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Chat;

public record MarkMessageAsReadCommand(int MessageId, int RoomId, int UserId, int? LastMessageId) : IRequest<ApiResponse<string>>;

public class MarkMessageAsReadCommandValidator : AbstractValidator<MarkMessageAsReadCommand>
{
    public MarkMessageAsReadCommandValidator()
    {
        RuleFor(x => x.MessageId).GreaterThan(0);
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.LastMessageId).GreaterThan(0).When(x => x.LastMessageId.HasValue);
    }
}

public class MarkMessageAsReadCommandHandler : IRequestHandler<MarkMessageAsReadCommand, ApiResponse<string>>
{
    private readonly IRepository<MessageStatus> _statusRepository;
    private readonly IRepository<ChatParticipant> _participantRepository;
    private readonly IRepository<ChatMessage> _messageRepository;
    private readonly IValidator<MarkMessageAsReadCommand> _validator;
    private readonly ILogger<MarkMessageAsReadCommandHandler> _logger;

    public MarkMessageAsReadCommandHandler(
        IRepository<MessageStatus> statusRepository,
        IRepository<ChatParticipant> participantRepository,
        IRepository<ChatMessage> messageRepository,
        IValidator<MarkMessageAsReadCommand> validator,
        ILogger<MarkMessageAsReadCommandHandler> logger)
    {
        _statusRepository = statusRepository;
        _participantRepository = participantRepository;
        _messageRepository = messageRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(MarkMessageAsReadCommand request, CancellationToken cancellationToken)
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

            // Verify user is participant
            var isParticipant = await _participantRepository.Query()
                .AnyAsync(p => p.RoomId == request.RoomId && p.UserId == request.UserId, cancellationToken);

            if (!isParticipant)
            {
                return ApiResponse<string>.Failure("You are not a participant of this chat room", 403);
            }

            // If LastMessageId is provided, mark all messages up to that point as read
            if (request.LastMessageId.HasValue)
            {
                var messagesToMarkRead = await _messageRepository.Query()
                    .Where(m => m.RoomId == request.RoomId && m.Id <= request.LastMessageId.Value)
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken);

                var statusesToUpdate = await _statusRepository.Query()
                    .Where(ms => messagesToMarkRead.Contains(ms.MessageId) &&
                                ms.UserId == request.UserId &&
                                ms.Status != MessageStatusType.Read)
                    .ToListAsync(cancellationToken);

                foreach (var status in statusesToUpdate)
                {
                    status.Status = MessageStatusType.Read;
                    status.Timestamp = DateTime.UtcNow;
                }

                if (statusesToUpdate.Any())
                {
                    await _statusRepository.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Marked {Count} messages as read for user {UserId} in room {RoomId}",
                        statusesToUpdate.Count, request.UserId, request.RoomId);
                }
            }
            else
            {
                // Mark single message as read
                var messageStatus = await _statusRepository.Query()
                    .FirstOrDefaultAsync(ms => ms.MessageId == request.MessageId && ms.UserId == request.UserId, cancellationToken);

                if (messageStatus != null)
                {
                    messageStatus.Status = MessageStatusType.Read;
                    messageStatus.Timestamp = DateTime.UtcNow;
                    _statusRepository.Update(messageStatus);
                    await _statusRepository.SaveChangesAsync(cancellationToken);
                }
            }

            return ApiResponse<string>.SuccessResponse("Messages marked as read successfully", 200);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking messages as read for user {UserId}", request.UserId);
            return ApiResponse<string>.Failure("An error occurred while marking messages as read", 500);
        }
    }
}