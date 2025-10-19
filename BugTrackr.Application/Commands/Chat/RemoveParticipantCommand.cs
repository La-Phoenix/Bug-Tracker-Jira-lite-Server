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

public record RemoveParticipantCommand(int RoomId, int ParticipantUserId, int RequesterId) : IRequest<ApiResponse<string>>;

public class RemoveParticipantCommandValidator : AbstractValidator<RemoveParticipantCommand>
{
    public RemoveParticipantCommandValidator()
    {
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.ParticipantUserId).GreaterThan(0);
        RuleFor(x => x.RequesterId).GreaterThan(0);
    }
}

public class RemoveParticipantCommandHandler : IRequestHandler<RemoveParticipantCommand, ApiResponse<string>>
{
    private readonly IRepository<ChatParticipant> _participantRepository;
    private readonly IValidator<RemoveParticipantCommand> _validator;
    public readonly IChatNotificationService _notificationService;
    private readonly ILogger<RemoveParticipantCommandHandler> _logger;

    public RemoveParticipantCommandHandler(
        IRepository<ChatParticipant> participantRepository,
        IChatNotificationService notificationService,
        IValidator<RemoveParticipantCommand> validator,
        ILogger<RemoveParticipantCommandHandler> logger)
    {
        _participantRepository = participantRepository;
        _validator = validator;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(RemoveParticipantCommand request, CancellationToken cancellationToken)
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

            // Get requester participant info
            var requesterParticipant = await _participantRepository.Query()
                .FirstOrDefaultAsync(p => p.RoomId == request.RoomId && p.UserId == request.RequesterId, cancellationToken);

            if (requesterParticipant == null)
            {
                return ApiResponse<string>.Failure("You are not a participant of this chat room", 403);
            }

            // Get participant to remove
            var participantToRemove = await _participantRepository.Query()
                .FirstOrDefaultAsync(p => p.RoomId == request.RoomId && p.UserId == request.ParticipantUserId, cancellationToken);

            if (participantToRemove == null)
            {
                return ApiResponse<string>.Failure("User is not a participant of this chat room", 404);
            }

            // Check permissions - users can remove themselves, or admins/moderators can remove others
            bool canRemove = request.RequesterId == request.ParticipantUserId || // Self removal
                           requesterParticipant.Role == ChatParticipantRole.Admin ||
                           (requesterParticipant.Role == ChatParticipantRole.Moderator && participantToRemove.Role == ChatParticipantRole.Member);

            if (!canRemove)
            {
                return ApiResponse<string>.Failure("Insufficient permissions to remove this participant", 403);
            }

            _participantRepository.Delete(participantToRemove);
            await _participantRepository.SaveChangesAsync(cancellationToken);

            // ✅ Notify everyone in the room about removal
            await _notificationService.NotifyParticipantRemoved(request.RoomId, request.ParticipantUserId);

            // ✅ Notify the removed user directly so they can leave the room view
            await _notificationService.NotifyRemovedFromRoom(request.RoomId, request.ParticipantUserId);

            _logger.LogInformation("Removed participant {UserId} from room {RoomId}", request.ParticipantUserId, request.RoomId);
            return ApiResponse<string>.SuccessResponse("Participant removed successfully", 200);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing participant {UserId} from room {RoomId}", request.ParticipantUserId, request.RoomId);
            return ApiResponse<string>.Failure("An error occurred while removing the participant", 500);
        }
    }
}