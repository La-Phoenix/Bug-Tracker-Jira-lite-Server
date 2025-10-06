using BugTrackr.Application.Common;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Chat;

public record TogglePinCommand(int RoomId, int UserId, bool IsPinned) : IRequest<ApiResponse<string>>;

public class TogglePinCommandValidator : AbstractValidator<TogglePinCommand>
{
    public TogglePinCommandValidator()
    {
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}

public class TogglePinCommandHandler : IRequestHandler<TogglePinCommand, ApiResponse<string>>
{
    private readonly IRepository<ChatParticipant> _participantRepository;
    private readonly IValidator<TogglePinCommand> _validator;
    private readonly ILogger<TogglePinCommandHandler> _logger;

    public TogglePinCommandHandler(
        IRepository<ChatParticipant> participantRepository,
        IValidator<TogglePinCommand> validator,
        ILogger<TogglePinCommandHandler> logger)
    {
        _participantRepository = participantRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(TogglePinCommand request, CancellationToken cancellationToken)
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

            var participant = await _participantRepository.Query()
                .FirstOrDefaultAsync(p => p.RoomId == request.RoomId && p.UserId == request.UserId, cancellationToken);

            if (participant == null)
            {
                return ApiResponse<string>.Failure("You are not a participant of this chat room", 404);
            }

            participant.IsPinned = request.IsPinned;
            _participantRepository.Update(participant);
            await _participantRepository.SaveChangesAsync(cancellationToken);

            var action = request.IsPinned ? "pinned" : "unpinned";
            _logger.LogInformation("User {UserId} {Action} room {RoomId}", request.UserId, action, request.RoomId);
            return ApiResponse<string>.SuccessResponse($"Chat room {action} successfully", 200);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling pin for room {RoomId} and user {UserId}", request.RoomId, request.UserId);
            return ApiResponse<string>.Failure("An error occurred while updating pin status", 500);
        }
    }
}
