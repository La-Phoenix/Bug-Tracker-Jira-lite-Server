using BugTrackr.Application.Dtos.Chat;
using BugTrackr.Application.Services.Chat;
using BugTrackr.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using BugTrackr.Application.Services.NotificationService;

namespace BugTrackr.Infrastructure.Chat;

public class ChatNotificationService : IChatNotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly INotificationService _notificationService;
    private readonly IRepository<ChatParticipant> _participantRepository;
    private readonly IRepository<User> _userRepository;

    public ChatNotificationService(
        IHubContext<ChatHub> hubContext,
        INotificationService notificationService,
        IRepository<ChatParticipant> participantRepository,
        IRepository<User> userRepository)
    {
        _hubContext = hubContext;
        _notificationService = notificationService;
        _participantRepository = participantRepository;
        _userRepository = userRepository;
    }

    public async Task NotifyNewMessage(int roomId, ChatMessageDto message, CancellationToken cancellation)
    {
        await _hubContext.Clients.Group($"room_{roomId}")
            .SendAsync("MessageReceived", new { RoomId = roomId, Message = message });

        // Get room participants for notifications
        var participants = await _participantRepository.Query()
            .Include(p => p.User)
            .Include(p => p.Room)
            .Where(p => p.RoomId == roomId)
            .ToListAsync();

        if (participants.Any())
        {
            var room = participants.First().Room;
            var users = participants.Select(p => p.User).ToList();

            var sender = await _userRepository.GetByIdAsync(message.SenderId);
            if (sender == null) return;
            

            // Create a ChatMessage entity from the DTO for the notification
            var chatMessage = new ChatMessage
            {
                Id = message.Id,
                SenderId = message.SenderId,
                Content = message.Content,
                Sender = sender,
            };

            // Send persistent notifications
            await _notificationService.SendChatMessageNotification(users, chatMessage, room, cancellation);
        }
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

    public async Task NotifyParticipantAdded(int roomId, ChatParticipantDto participant, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group($"room_{roomId}")
            .SendAsync("ParticipantAdded", new { RoomId = roomId, Participant = participant });

        // Get room and inviter information for notification
        var roomData = await _participantRepository.Query()
            .Include(p => p.Room)
            .Where(p => p.RoomId == roomId)
            .FirstOrDefaultAsync();

        if (roomData?.Room != null)
        {
            var user = await _userRepository.GetByIdAsync(participant.UserId);
            var inviter = await _userRepository.GetByIdAsync(roomData.Room.CreatedBy); // Assuming room creator is the inviter

            if (user != null && inviter != null)
            {
                await _notificationService.SendChatInvitationNotification(user, roomData.Room, inviter, cancellationToken);
            }
        }
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