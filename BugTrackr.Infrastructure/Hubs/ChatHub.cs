using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Collections.Concurrent;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Infrastructure.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IRepository<ChatParticipant> _participantRepository;
    private readonly IRepository<TypingStatus> _typingRepository;
    private readonly ILogger<ChatHub> _logger;

    // Store user connections - in production use Redis or database
    private static readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();
    private static readonly ConcurrentDictionary<string, HashSet<string>> _roomConnections = new();

    public ChatHub(
        IRepository<ChatParticipant> participantRepository,
        IRepository<TypingStatus> typingRepository,
        ILogger<ChatHub> logger)
    {
        _participantRepository = participantRepository;
        _typingRepository = typingRepository;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId != null)
        {
            // Add connection to user tracking
            _userConnections.AddOrUpdate(userId,
                new HashSet<string> { Context.ConnectionId },
                (key, existing) => { existing.Add(Context.ConnectionId); return existing; });

            // Notify others that user is online
            await Clients.All.SendAsync("UserStatusChanged", new { UserId = userId, IsOnline = true });

            _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId != null)
        {
            // Remove connection
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(Context.ConnectionId);
                if (!connections.Any())
                {
                    _userConnections.TryRemove(userId, out _);
                    // User has no more connections - mark as offline
                    await Clients.All.SendAsync("UserStatusChanged", new { UserId = userId, IsOnline = false, LastSeen = DateTime.UtcNow });
                }
            }

            // Remove from all room connections
            foreach (var roomConnections in _roomConnections.Values)
            {
                roomConnections.Remove(Context.ConnectionId);
            }

            _logger.LogInformation("User {UserId} disconnected from connection {ConnectionId}", userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a chat room to receive real-time messages
    /// </summary>
    public async Task JoinRoom(int roomId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        // Verify user is participant
        var isParticipant = await _participantRepository.Query()
            .AnyAsync(p => p.RoomId == roomId && p.UserId == int.Parse(userId));

        if (!isParticipant)
        {
            await Clients.Caller.SendAsync("Error", "You are not a participant of this room");
            return;
        }

        var roomGroup = GetRoomGroup(roomId);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomGroup);

        // Track room connections
        var roomKey = roomId.ToString();
        _roomConnections.AddOrUpdate(roomKey,
            new HashSet<string> { Context.ConnectionId },
            (key, existing) => { existing.Add(Context.ConnectionId); return existing; });

        await Clients.Caller.SendAsync("JoinedRoom", roomId);
        _logger.LogInformation("User {UserId} joined room {RoomId}", userId, roomId);
    }

    /// <summary>
    /// Leave a chat room
    /// </summary>
    public async Task LeaveRoom(int roomId)
    {
        var roomGroup = GetRoomGroup(roomId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomGroup);

        // Remove from room connections
        var roomKey = roomId.ToString();
        if (_roomConnections.TryGetValue(roomKey, out var connections))
        {
            connections.Remove(Context.ConnectionId);
        }

        await Clients.Caller.SendAsync("LeftRoom", roomId);
        _logger.LogInformation("User left room {RoomId}", roomId);
    }

    /// <summary>
    /// Handle typing indicators
    /// </summary>
    public async Task StartTyping(int roomId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var roomGroup = GetRoomGroup(roomId);
        await Clients.OthersInGroup(roomGroup).SendAsync("UserTyping", new
        {
            RoomId = roomId,
            UserId = userId,
            IsTyping = true,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task StopTyping(int roomId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        var roomGroup = GetRoomGroup(roomId);
        await Clients.OthersInGroup(roomGroup).SendAsync("UserTyping", new
        {
            RoomId = roomId,
            UserId = userId,
            IsTyping = false,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get online users in a room
    /// </summary>
    public async Task GetOnlineUsers(int roomId)
    {
        var roomKey = roomId.ToString();
        if (_roomConnections.TryGetValue(roomKey, out var connections))
        {
            var onlineUsers = new List<string>();

            foreach (var userConnections in _userConnections)
            {
                if (userConnections.Value.Any(conn => connections.Contains(conn)))
                {
                    onlineUsers.Add(userConnections.Key);
                }
            }

            await Clients.Caller.SendAsync("OnlineUsersInRoom", new { RoomId = roomId, OnlineUsers = onlineUsers });
        }
    }

    private string? GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private static string GetRoomGroup(int roomId) => $"room_{roomId}";
}