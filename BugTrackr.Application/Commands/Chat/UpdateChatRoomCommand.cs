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

public record UpdateChatRoomCommand(int RoomId, UpdateChatRoomDto UpdateData, int UserId) : IRequest<ApiResponse<ChatRoomDto>>;

public class UpdateChatRoomCommandValidator : AbstractValidator<UpdateChatRoomCommand>
{
    public UpdateChatRoomCommandValidator()
    {
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);

        RuleFor(x => x.UpdateData.Name)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.UpdateData.Name));

        RuleFor(x => x.UpdateData.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.UpdateData.Description));
    }
}

public class UpdateChatRoomCommandHandler : IRequestHandler<UpdateChatRoomCommand, ApiResponse<ChatRoomDto>>
{
    private readonly IRepository<ChatRoom> _roomRepository;
    private readonly IRepository<ChatParticipant> _participantRepository;
    private readonly IChatNotificationService _notificationService;
    private readonly IValidator<UpdateChatRoomCommand> _validator;
    private readonly ILogger<UpdateChatRoomCommandHandler> _logger;

    public UpdateChatRoomCommandHandler(
        IRepository<ChatRoom> roomRepository,
        IRepository<ChatParticipant> participantRepository,
        IChatNotificationService notificationService,
        IValidator<UpdateChatRoomCommand> validator,
        ILogger<UpdateChatRoomCommandHandler> logger)
    {
        _roomRepository = roomRepository;
        _participantRepository = participantRepository;
        _notificationService = notificationService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<ChatRoomDto>> Handle(UpdateChatRoomCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(err => err.ErrorMessage);
                var resp = ApiResponse<ChatRoomDto>.Failure("Validation failed", 400);
                resp.Errors = errors;
                return resp;
            }

            var room = await _roomRepository.Query()
                .Include(r => r.Participants)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken);

            if (room == null)
            {
                return ApiResponse<ChatRoomDto>.Failure("Chat room not found", 404);
            }

            // Check if user is admin or moderator
            var userParticipant = room.Participants.FirstOrDefault(p => p.UserId == request.UserId);
            if (userParticipant == null)
            {
                return ApiResponse<ChatRoomDto>.Failure("You are not a participant of this chat room", 403);
            }

            if (userParticipant.Role != ChatParticipantRole.Admin && userParticipant.Role != ChatParticipantRole.Moderator)
            {
                return ApiResponse<ChatRoomDto>.Failure("Only admins and moderators can update room details", 403);
            }

            // Update room properties
            if (!string.IsNullOrEmpty(request.UpdateData.Name))
                room.Name = request.UpdateData.Name;

            if (request.UpdateData.Description != null)
                room.Description = request.UpdateData.Description;

            if (!string.IsNullOrEmpty(request.UpdateData.Avatar))
                room.Avatar = request.UpdateData.Avatar;

            room.UpdatedAt = DateTime.UtcNow;

            _roomRepository.Update(room);
            await _roomRepository.SaveChangesAsync(cancellationToken);

            var roomDto = MapToDto(room);

            // Notify participants about room update
            await _notificationService.NotifyRoomUpdated(roomDto);

            _logger.LogInformation("Chat room {RoomId} updated by user {UserId}", request.RoomId, request.UserId);
            return ApiResponse<ChatRoomDto>.SuccessResponse(roomDto, 200, "Chat room updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating chat room {RoomId}", request.RoomId);
            return ApiResponse<ChatRoomDto>.Failure("An error occurred while updating the chat room", 500);
        }
    }

    private static ChatRoomDto MapToDto(ChatRoom room)
    {
        return new ChatRoomDto
        {
            Id = room.Id,
            Name = room.Name,
            Type = room.Type.ToString().ToLowerInvariant(),
            Description = room.Description,
            Avatar = room.Avatar,
            ProjectId = room.ProjectId,
            CreatedBy = room.CreatedBy,
            IsPrivate = room.IsPrivate,
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt,
            Participants = room.Participants.Select(p => new ChatParticipantDto
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
            }).ToList()
        };
    }
}