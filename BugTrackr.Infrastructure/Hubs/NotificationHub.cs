using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Infrastructure.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    // Store user connections - in production use Redis or database
    private static readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();

    public NotificationHub(ILogger<NotificationHub> logger)
    {
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

            // Join user to their personal notification group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            _logger.LogInformation("User {UserId} connected to notifications with connection {ConnectionId}",
                userId, Context.ConnectionId);
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
                }
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

            _logger.LogInformation("User {UserId} disconnected from notifications", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    public async Task MarkAsRead(string notificationId)
    {
        var userId = GetUserId();
        if (userId == null) return;

        // Here you would typically update the notification status in your database
        // For now, just acknowledge
        await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
        _logger.LogInformation("Notification {NotificationId} marked as read by user {UserId}", notificationId, userId);
    }

    /// <summary>
    /// Get online status of a user
    /// </summary>
    public static bool IsUserOnline(string userId)
    {
        return _userConnections.ContainsKey(userId);
    }

    private string? GetUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}