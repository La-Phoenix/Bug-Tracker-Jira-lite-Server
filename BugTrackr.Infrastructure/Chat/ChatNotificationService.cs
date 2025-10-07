using BugTrackr.Application.Dtos.Chat;
using BugTrackr.Application.Services;
using BugTrackr.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BugTrackr.Infrastructure.Chat;

public class ChatNotificationService : IChatNotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatNotificationService(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyNewMessage(int roomId, ChatMessageDto message)
    {
        await _hubContext.Clients.Group($"room_{roomId}")
            .SendAsync("MessageReceived", new { RoomId = roomId, Message = message });
    }

    public async Task NotifyMessageEdited(int roomId, int messageId, string newContent)
    {
        await _hubContext.Clients.Group($"room_{roomId}")
            .SendAsync("MessageEdited", new
            {
                RoomId = roomId,
                MessageId = messageId,
                Content = newContent,
                EditedAt = DateTime.UtcNow
            });
    }

    public async Task NotifyMessageDeleted(int roomId, int messageId)
    {
        await _hubContext.Clients.Group($"room_{roomId}")
            .SendAsync("MessageDeleted", new { RoomId = roomId, MessageId = messageId });
    }

    public async Task NotifyParticipantAdded(int roomId, ChatParticipantDto participant)
    {
        await _hubContext.Clients.Group($"room_{roomId}")
            .SendAsync("ParticipantAdded", new { RoomId = roomId, Participant = participant });
    }

    public async Task NotifyRoomCreated(ChatRoomDto room)
    {
        foreach (var participant in room.Participants)
        {
            await _hubContext.Clients.User(participant.UserId.ToString())
                .SendAsync("RoomCreated", new { Room = room });
        }
    }


    public async Task NotifyParticipantRemoved(int roomId, int userId)
    {
        await _hubContext.Clients.Group($"room_{roomId}")
            .SendAsync("ParticipantRemoved", new { RoomId = roomId, UserId = userId });
    }
    public async Task NotifyRemovedFromRoom(int roomId, int participantUserId)
    {
        await _hubContext.Clients.User(participantUserId.ToString())
                .SendAsync("RemovedFromRoom", new { RoomId = roomId });
    }



    public async Task NotifyRoomUpdated(ChatRoomDto room)
    {
        await _hubContext.Clients.Group($"room_{room.Id}")
            .SendAsync("RoomUpdated", new { Room = room });
    }
}