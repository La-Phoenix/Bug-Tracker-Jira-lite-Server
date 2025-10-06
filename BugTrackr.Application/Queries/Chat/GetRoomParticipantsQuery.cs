using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.Chat;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Queries.Chat;

public record GetRoomParticipantsQuery(int RoomId, int UserId) : IRequest<ApiResponse<List<ChatParticipantDto>>>;

public class GetRoomParticipantsQueryValidator : AbstractValidator<GetRoomParticipantsQuery>
{
    public GetRoomParticipantsQueryValidator()
    {
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}

public class GetRoomParticipantsQueryHandler : IRequestHandler<GetRoomParticipantsQuery, ApiResponse<List<ChatParticipantDto>>>
{
    private readonly IRepository<ChatParticipant> _participantRepository;
    private readonly IValidator<GetRoomParticipantsQuery> _validator;
    private readonly ILogger<GetRoomParticipantsQueryHandler> _logger;

    public GetRoomParticipantsQueryHandler(
        IRepository<ChatParticipant> participantRepository,
        IValidator<GetRoomParticipantsQuery> validator,
        ILogger<GetRoomParticipantsQueryHandler> logger)
    {
        _participantRepository = participantRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ChatParticipantDto>>> Handle(GetRoomParticipantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(err => err.ErrorMessage);
                var resp = ApiResponse<List<ChatParticipantDto>>.Failure("Validation failed", 400);
                resp.Errors = errors;
                return resp;
            }

            // Verify user is participant
            var isParticipant = await _participantRepository.Query()
                .AnyAsync(p => p.RoomId == request.RoomId && p.UserId == request.UserId, cancellationToken);

            if (!isParticipant)
            {
                return ApiResponse<List<ChatParticipantDto>>.Failure("You are not a participant of this chat room", 403);
            }

            var participants = await _participantRepository.Query()
                .Where(p => p.RoomId == request.RoomId)
                .Include(p => p.User)
                .OrderBy(p => p.Role)
                .ThenBy(p => p.User.Name)
                .ToListAsync(cancellationToken);

            var participantDtos = participants.Select(p => new ChatParticipantDto
            {
                Id = p.Id,
                RoomId = p.RoomId,
                UserId = p.UserId,
                UserName = p.User.Name,
                UserEmail = p.User.Email,
                Role = p.Role.ToString(),
                JoinedAt = p.JoinedAt,
                LastSeenAt = p.LastSeenAt,
                IsPinned = p.IsPinned,
                IsMuted = p.IsMuted,
                IsOnline = false // This would be set by SignalR or presence service
            }).ToList();

            return ApiResponse<List<ChatParticipantDto>>.SuccessResponse(participantDtos, 200, "Participants retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving participants for room {RoomId}", request.RoomId);
            return ApiResponse<List<ChatParticipantDto>>.Failure("An error occurred while retrieving participants", 500);
        }
    }
}