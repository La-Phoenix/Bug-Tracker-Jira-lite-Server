using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.Chat;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Queries.Chat;

public record GetChatRoomsQuery(int UserId) : IRequest<ApiResponse<List<ChatRoomDto>>>;

public class GetChatRoomsQueryHandler : IRequestHandler<GetChatRoomsQuery, ApiResponse<List<ChatRoomDto>>>
{
    private readonly IRepository<ChatParticipant> _participantRepository;
    private readonly IRepository<ChatMessage> _messageRepository;
    private readonly ILogger<GetChatRoomsQueryHandler> _logger;

    public GetChatRoomsQueryHandler(
        IRepository<ChatParticipant> participantRepository,
        IRepository<ChatMessage> messageRepository,
        ILogger<GetChatRoomsQueryHandler> logger)
    {
        _participantRepository = participantRepository;
        _messageRepository = messageRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ChatRoomDto>>> Handle(GetChatRoomsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get user's chat rooms with proper includes
            var userParticipations = await _participantRepository.Query()
                .Where(p => p.UserId == request.UserId)
                .Include(p => p.Room)
                .ThenInclude(r => r.Participants)
                .ThenInclude(p => p.User)
                .ToListAsync(cancellationToken);

            var roomDtos = new List<ChatRoomDto>();

            foreach (var participation in userParticipations)
            {
                var room = participation.Room;

                // Get last message separately to avoid complex LINQ
                var lastMessage = await _messageRepository.Query()
                    .Where(m => m.RoomId == room.Id)
                    .Include(m => m.Sender)
                    .OrderBy(m => m.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                // Count unread messages for this user
                var unreadCount = await _messageRepository.Query()
                    .Where(m => m.RoomId == room.Id)
                    .Where(m => !m.MessageStatuses.Any(ms => ms.UserId == request.UserId && ms.Status == Domain.Enums.MessageStatusType.Read))
                    .CountAsync(cancellationToken);

                var roomDto = new ChatRoomDto
                {
                    Id = room.Id,
                    Name = room.Name,
                    Type = room.Type.ToString().ToLowerInvariant(),
                    Description = room.Description,
                    Avatar = room.Avatar,
                    ProjectId = room.ProjectId,
                    CreatedBy = room.CreatedBy,
                    IsPrivate = room.IsPrivate,
                    IsPinned = participation.IsPinned,
                    IsMuted = participation.IsMuted,
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
                        IsMuted = p.IsMuted,
                        IsOnline = false // This would be set by SignalR
                    }).ToList(),
                    LastMessage = lastMessage != null ? new ChatMessageDto
                    {
                        Id = lastMessage.Id,
                        RoomId = lastMessage.RoomId,
                        SenderId = lastMessage.SenderId,
                        SenderName = lastMessage.Sender.Name,
                        Content = lastMessage.Content,
                        Type = lastMessage.Type.ToString().ToLowerInvariant(),
                        CreatedAt = lastMessage.CreatedAt,
                        UpdatedAt = lastMessage.UpdatedAt,
                        IsEdited = lastMessage.IsEdited,
                        EditedAt = lastMessage.EditedAt
                    } : null,
                    UnreadCount = unreadCount
                };

                roomDtos.Add(roomDto);
            }

            // Sort by last message date or creation date
            var sortedRooms = roomDtos
                .OrderByDescending(r => r.LastMessage?.CreatedAt ?? r.CreatedAt)
                .ToList();

            return ApiResponse<List<ChatRoomDto>>.SuccessResponse(sortedRooms, 200, "Chat rooms retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat rooms for user {UserId}", request.UserId);
            return ApiResponse<List<ChatRoomDto>>.Failure("An error occurred while retrieving chat rooms", 500);
        }
    }
}