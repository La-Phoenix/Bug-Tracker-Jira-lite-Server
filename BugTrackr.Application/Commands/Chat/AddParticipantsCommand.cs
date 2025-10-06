using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.Chat;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using BugTrackr.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Chat;

public record AddParticipantsCommand(int RoomId, List<int> UserIds, int RequesterId) : IRequest<ApiResponse<List<ChatParticipantDto>>>;

public class AddParticipantsCommandValidator : AbstractValidator<AddParticipantsCommand>
{
    public AddParticipantsCommandValidator()
    {
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.RequesterId).GreaterThan(0);
        RuleFor(x => x.UserIds)
            .NotEmpty()
            .Must(userIds => userIds.All(id => id > 0))
            .WithMessage("All user IDs must be greater than 0");
    }
}

public class AddParticipantsCommandHandler : IRequestHandler<AddParticipantsCommand, ApiResponse<List<ChatParticipantDto>>>
{
    private readonly IRepository<ChatParticipant> _participantRepository;
    private readonly IRepository<ChatRoom> _roomRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IValidator<AddParticipantsCommand> _validator;
    private readonly ILogger<AddParticipantsCommandHandler> _logger;

    public AddParticipantsCommandHandler(
        IRepository<ChatParticipant> participantRepository,
        IRepository<ChatRoom> roomRepository,
        IRepository<User> userRepository,
        IValidator<AddParticipantsCommand> validator,
        ILogger<AddParticipantsCommandHandler> logger)
    {
        _participantRepository = participantRepository;
        _roomRepository = roomRepository;
        _userRepository = userRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ChatParticipantDto>>> Handle(AddParticipantsCommand request, CancellationToken cancellationToken)
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

            // Verify requester is admin or moderator
            var requesterParticipant = await _participantRepository.Query()
                .FirstOrDefaultAsync(p => p.RoomId == request.RoomId && p.UserId == request.RequesterId, cancellationToken);

            if (requesterParticipant == null)
            {
                return ApiResponse<List<ChatParticipantDto>>.Failure("You are not a participant of this chat room", 403);
            }

            if (requesterParticipant.Role != ChatParticipantRole.Admin && requesterParticipant.Role != ChatParticipantRole.Moderator)
            {
                return ApiResponse<List<ChatParticipantDto>>.Failure("Only admins and moderators can add participants", 403);
            }

            // Verify users exist
            var existingUsers = await _userRepository.Query()
                .Where(u => request.UserIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            if (existingUsers.Count != request.UserIds.Count)
            {
                return ApiResponse<List<ChatParticipantDto>>.Failure("Some users don't exist", 400);
            }

            // Check for already existing participants
            var existingParticipants = await _participantRepository.Query()
                .Where(p => p.RoomId == request.RoomId && request.UserIds.Contains(p.UserId))
                .Select(p => p.UserId)
                .ToListAsync(cancellationToken);

            var newUserIds = request.UserIds.Except(existingParticipants).ToList();

            if (!newUserIds.Any())
            {
                return ApiResponse<List<ChatParticipantDto>>.Failure("All users are already participants", 400);
            }

            // Create new participants
            var newParticipants = newUserIds.Select(userId => new ChatParticipant
            {
                RoomId = request.RoomId,
                UserId = userId,
                Role = ChatParticipantRole.Member,
                JoinedAt = DateTime.UtcNow,
                IsPinned = false,
                IsMuted = false
            }).ToList();

            await _participantRepository.AddRangeAsync(newParticipants);
            await _participantRepository.SaveChangesAsync(cancellationToken);

            // Load created participants with user info
            var createdParticipants = await _participantRepository.Query()
                .Where(p => p.RoomId == request.RoomId && newUserIds.Contains(p.UserId))
                .Include(p => p.User)
                .ToListAsync(cancellationToken);

            var participantDtos = createdParticipants.Select(p => new ChatParticipantDto
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
                IsMuted = p.IsMuted
            }).ToList();

            return ApiResponse<List<ChatParticipantDto>>.SuccessResponse(participantDtos, 201, "Participants added successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding participants to room {RoomId}", request.RoomId);
            return ApiResponse<List<ChatParticipantDto>>.Failure("An error occurred while adding participants", 500);
        }
    }
}