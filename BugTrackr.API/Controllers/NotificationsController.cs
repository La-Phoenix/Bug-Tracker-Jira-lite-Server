using BugTrackr.Application.Common;
using BugTrackr.Application.Services.NotificationService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BugTrackr.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get user notifications
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        try
        {
            var notifications = await _notificationService.GetUserNotifications(userId, unreadOnly, limit);
            return Ok(ApiResponse<object>.SuccessResponse(notifications, 200, "Notifications retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications for user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Failure("Failed to retrieve notifications", 500));
        }
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    [HttpPatch("{notificationId:int}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        try
        {
            await _notificationService.MarkNotificationAsRead(notificationId, userId, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(null, 200, "Notification marked as read"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as read", notificationId);
            return StatusCode(500, ApiResponse<object>.Failure("Failed to mark notification as read", 500));
        }
    }

    /// <summary>
    /// Mark all notifications as read
    /// </summary>
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        try
        {
            await _notificationService.MarkAllNotificationsAsRead(userId, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(null, 200, "All notifications marked as read"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Failure("Failed to mark all notifications as read", 500));
        }
    }

    /// <summary>
    /// Delete notification
    /// </summary>
    [HttpDelete("{notificationId:int}")]
    public async Task<IActionResult> DeleteNotification(int notificationId, CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        try
        {
            await _notificationService.DeleteNotification(notificationId, userId, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(null, 200, "Notification deleted"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification {NotificationId}", notificationId);
            return StatusCode(500, ApiResponse<object>.Failure("Failed to delete notification", 500));
        }
    }
}