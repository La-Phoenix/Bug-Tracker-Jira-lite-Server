using BugTrackr.Application.Services;
using BugTrackr.Application.Services.Email;
using BugTrackr.Application.Services.NotificationService;
using BugTrackr.Domain.Entities;
using BugTrackr.Domain.Enums;
using BugTrackr.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BugTrackr.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly IEmailService _emailService;
    private readonly IRepository<Notification> _notificationRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<NotificationHub> notificationHub,
        IEmailService emailService,
        IRepository<Notification> notificationRepository,
        ILogger<NotificationService> logger)
    {
        _notificationHub = notificationHub;
        _emailService = emailService;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task SendIssueAssignedNotification(User assignee, Issue issue, User assignedBy, CancellationToken cancellationToken)
    {
        try
        {
            var notification = new Notification
            {
                UserId = assignee.Id,
                Type = NotificationType.IssueAssigned,
                Title = "Issue Assigned",
                Message = $"You have been assigned to '{issue.Title}' by {assignedBy.Name}",
                Data = JsonSerializer.Serialize(new { IssueId = issue.Id, ProjectId = issue.ProjectId, AssignedBy = assignedBy.Id }),
                Priority = issue.Priority.Name switch
                {
                    "Critical" => PriorityLevel.Critical,
                    "High" => PriorityLevel.High,
                    "Medium" => PriorityLevel.Medium,
                    "Low" => PriorityLevel.Low,
                    _ => PriorityLevel.Medium
                },
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            await SaveAndSendNotification(notification, cancellationToken);

            // Send email if user has assignment notifications enabled
            if (assignee.AssignmentNotifications && assignee.EmailNotifications)
            {
                await _emailService.SendIssueAssignedEmailAsync(assignee, issue, assignedBy);
            }

            _logger.LogInformation("Issue assignment notification sent to user {UserId} for issue {IssueId}",
                assignee.Id, issue.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send issue assignment notification");
        }
    }

    public async Task SendCommentNotification(List<User> recipients, Issue issue, Comment comment, CancellationToken cancellation)
    {
        try
        {
            var notifications = recipients.Select(recipient => new Notification
            {
                UserId = recipient.Id,
                Type = NotificationType.IssueCommented,
                Title = "New Comment",
                Message = $"{comment.Author.Name} commented on '{issue.Title}'",
                Data = JsonSerializer.Serialize(new { IssueId = issue.Id, ProjectId = issue.ProjectId, CommentId = comment.Id }),
                Priority = PriorityLevel.Medium,
                GroupKey = $"issue_comments_{issue.Id}",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            }).ToList();

            await SaveAndSendBulkNotifications(notifications, cancellation);

            // Send emails to users who have comment notifications enabled
            var emailRecipients = recipients.Where(u => u.CommentNotifications && u.EmailNotifications).ToList();
            foreach (var recipient in emailRecipients)
            {
                await _emailService.SendCommentNotificationEmailAsync(recipient, issue, comment);
            }

            _logger.LogInformation("Comment notifications sent to {Count} recipients for issue {IssueId}",
                recipients.Count, issue.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send comment notifications");
        }
    }

    public async Task SendMentionNotification(User user, Issue issue, Comment comment, User mentionedBy, CancellationToken cancellation)
    {
        try
        {
            var notification = new Notification
            {
                UserId = user.Id,
                Type = NotificationType.UserMentioned,
                Title = "You were mentioned",
                Message = $"{mentionedBy.Name} mentioned you in '{issue.Title}'",
                Data = JsonSerializer.Serialize(new { IssueId = issue.Id, ProjectId = issue.ProjectId, CommentId = comment.Id, MentionedBy = mentionedBy.Id }),
                Priority = PriorityLevel.High,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            await SaveAndSendNotification(notification, cancellation);

            // Send email if user has mention alerts enabled
            if (user.MentionAlerts && user.EmailNotifications)
            {
                await _emailService.SendMentionNotificationEmailAsync(user, issue, comment, mentionedBy);
            }

            _logger.LogInformation("Mention notification sent to user {UserId} for issue {IssueId}",
                user.Id, issue.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send mention notification");
        }
    }

    public async Task SendProjectInvitationNotification(User user, Project project, User invitedBy, CancellationToken cancellation)
    {
        try
        {
            var notification = new Notification
            {
                UserId = user.Id,
                Type = NotificationType.ProjectInvitation,
                Title = "Project Invitation",
                Message = $"{invitedBy.Name} invited you to join '{project.Name}'",
                Data = JsonSerializer.Serialize(new { ProjectId = project.Id, InvitedBy = invitedBy.Id }),
                Priority = PriorityLevel.High,
                ExpiresAt = DateTime.UtcNow.AddDays(14) // Project invitations expire sooner
            };

            await SaveAndSendNotification(notification, cancellation);

            // Always send email for project invitations
            if (user.EmailNotifications)
            {
                await _emailService.SendProjectInvitationEmailAsync(user, project, invitedBy);
            }

            _logger.LogInformation("Project invitation notification sent to user {UserId} for project {ProjectId}",
                user.Id, project.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send project invitation notification");
        }
    }

    public async Task SendIssueCreatedNotification(List<User> projectMembers, Issue issue, User reporter, CancellationToken cancellation)
    {
        try
        {
            var recipients = projectMembers.Where(u => u.Id != reporter.Id).ToList();
            var notifications = recipients.Select(recipient => new Notification
            {
                UserId = recipient.Id,
                Type = NotificationType.IssueCreated,
                Title = "New Issue Created",
                Message = $"{reporter.Name} created '{issue.Title}' in {issue.Project?.Name}",
                Data = JsonSerializer.Serialize(new { IssueId = issue.Id, ProjectId = issue.ProjectId, ReporterId = reporter.Id }),
                Priority = issue.Priority.Name switch
                {
                    "Critical" => PriorityLevel.Critical,
                    "High" => PriorityLevel.High,
                    "Medium" => PriorityLevel.Medium,
                    "Low" => PriorityLevel.Low,
                    _ => PriorityLevel.Medium
                },
                GroupKey = $"project_issues_{issue.ProjectId}",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            }).ToList();

            await SaveAndSendBulkNotifications(notifications, cancellation);

            // Send emails only for high/critical priority issues
            var shouldEmail = issue.Priority?.Name is "High" or "Critical";
            if (shouldEmail)
            {
                var emailRecipients = recipients.Where(u => u.IssueUpdates && u.EmailNotifications).ToList();
                foreach (var recipient in emailRecipients)
                {
                    await _emailService.SendIssueCreatedEmailAsync(recipient, issue, reporter);
                }
            }

            _logger.LogInformation("Issue creation notifications sent to {Count} recipients for issue {IssueId}",
                recipients.Count, issue.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send issue creation notifications");
        }
    }

    public async Task SendIssueUpdatedNotification(List<User> watchers, Issue issue, string updateType, User updatedBy, CancellationToken cancellation)
    {
        try
        {
            var notifications = watchers.Select(watcher => new Notification
            {
                UserId = watcher.Id,
                Type = NotificationType.IssueUpdated,
                Title = "Issue Updated",
                Message = $"{updatedBy.Name} {updateType} '{issue.Title}'",
                Data = JsonSerializer.Serialize(new { IssueId = issue.Id, ProjectId = issue.ProjectId, UpdateType = updateType, UpdatedBy = updatedBy.Id }),
                Priority = PriorityLevel.Medium,
                GroupKey = $"issue_updates_{issue.Id}",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            }).ToList();

            await SaveAndSendBulkNotifications(notifications, cancellation);

            // Only send emails for major status changes
            var shouldEmail = updateType.Contains("status") &&
                             (updateType.Contains("closed") || updateType.Contains("resolved"));

            if (shouldEmail)
            {
                var emailRecipients = watchers.Where(u => u.IssueUpdates && u.EmailNotifications).ToList();
                foreach (var recipient in emailRecipients)
                {
                    await _emailService.SendIssueUpdatedEmailAsync(recipient, issue, updateType);
                }
            }

            _logger.LogInformation("Issue update notifications sent to {Count} recipients for issue {IssueId}",
                watchers.Count, issue.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send issue update notifications");
        }
    }

    public async Task SendChatMessageNotification(List<User> recipients, ChatMessage message, ChatRoom room, CancellationToken cancellationToken)
    {
        try
        {
            // Don't notify the sender
            var recipientsToNotify = recipients.Where(u => u.Id != message.SenderId).ToList();

            var notifications = recipientsToNotify.Select(recipient => new Notification
            {
                UserId = recipient.Id,
                Type = NotificationType.ChatMessage,
                Title = room.Type == ChatRoomType.Direct ? "New Message" : $"New message in {room.Name}",
                Message = $"{message.Sender.Name}: {(message.Content.Length > 50 ? message.Content[..50] + "..." : message.Content)}",
                Data = JsonSerializer.Serialize(new { RoomId = room.Id, MessageId = message.Id, SenderId = message.SenderId }),
                Priority = room.Type == ChatRoomType.Direct ? PriorityLevel.High : PriorityLevel.Medium,
                GroupKey = $"chat_{room.Id}",
                ExpiresAt = DateTime.UtcNow.AddDays(7) // Chat notifications expire sooner
            }).ToList();

            await SaveAndSendBulkNotifications(notifications, cancellationToken);

            // Send email notifications for chat messages based on user preferences and room type
            if (room.Type == ChatRoomType.Direct)
            {
                // For direct messages, send email to users who have email notifications enabled
                var emailRecipients = recipientsToNotify.Where(u => u.EmailNotifications).ToList();
                foreach (var recipient in emailRecipients)
                {
                    await _emailService.SendChatMessageEmailAsync(recipient, message, room);
                }
            }
            else
            {
                // For group chats, only send email if explicitly enabled for chat notifications
                // You might want to add a ChatNotifications property to User entity
                var emailRecipients = recipientsToNotify.Where(u => u.EmailNotifications).ToList();
                foreach (var recipient in emailRecipients)
                {
                    await _emailService.SendChatMessageEmailAsync(recipient, message, room);
                }
            }

            _logger.LogInformation("Chat message notifications sent to {Count} recipients for room {RoomId}",
                recipientsToNotify.Count, room.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send chat message notifications");
        }
    }

    public async Task SendChatInvitationNotification(User user, ChatRoom room, User invitedBy, CancellationToken cancellationToken)
    {
        try
        {
            var notification = new Notification
            {
                UserId = user.Id,
                Type = NotificationType.ChatInvitation,
                Title = "Chat Invitation",
                Message = room.Type == ChatRoomType.Direct
                    ? $"{invitedBy.Name} started a chat with you"
                    : $"{invitedBy.Name} added you to '{room.Name}'",
                Data = JsonSerializer.Serialize(new { RoomId = room.Id, InvitedBy = invitedBy.Id, RoomType = room.Type.ToString() }),
                Priority = PriorityLevel.Medium,
                ExpiresAt = DateTime.UtcNow.AddDays(14)
            };

            await SaveAndSendNotification(notification, cancellationToken);

            // Always send email for chat invitations (important)
            if (user.EmailNotifications)
            {
                await _emailService.SendChatInvitationEmailAsync(user, room, invitedBy);
            }

            _logger.LogInformation("Chat invitation notification sent to user {UserId} for room {RoomId}",
                user.Id, room.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send chat invitation notification");
        }
    }

    public async Task<List<Notification>> GetUserNotifications(int userId, bool unreadOnly = false, int limit = 50)
    {
        try
        {
            var query = _notificationRepository.Query()
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications for user {UserId}", userId);
            return new List<Notification>();
        }
    }

    public async Task MarkNotificationAsRead(int notificationId, int userId, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await _notificationRepository.Query()
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;

                _notificationRepository.Update(notification);
                await _notificationRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Notification {NotificationId} marked as read by user {UserId}",
                    notificationId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {NotificationId} as read", notificationId);
        }
    }

    public async Task MarkAllNotificationsAsRead(int userId, CancellationToken cancellationToken)
    {
        try
        {
            var unreadNotifications = await _notificationRepository.Query()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync(cancellationToken);

            if (!unreadNotifications.Any())
                return;

            var now = DateTime.UtcNow;

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
            }

            await _notificationRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", userId);
        }
    }

    public async Task DeleteNotification(int notificationId, int userId, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await _notificationRepository.Query()
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null)
            {
                _notificationRepository.Delete(notification);
                await _notificationRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Notification {NotificationId} deleted by user {UserId}",
                    notificationId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete notification {NotificationId}", notificationId);
        }
    }

    public async Task CleanupExpiredNotifications(CancellationToken cancellationToken)
    {
        try
        {
            var expiredNotifications = await _notificationRepository.Query()
                .Where(n => n.ExpiresAt != null && n.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            if (expiredNotifications.Any())
            {
                _notificationRepository.DeleteRange(expiredNotifications);
                await _notificationRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaned up {Count} expired notifications", expiredNotifications.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired notifications");
        }
    }

    private async Task SaveAndSendNotification(Notification notification, CancellationToken cancellationToken)
    {
        // Save to database
        await _notificationRepository.AddAsync(notification);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        // Send real-time notification
        var notificationData = new
        {
            Id = notification.Id,
            Type = notification.Type.ToString(),
            Title = notification.Title,
            Message = notification.Message,
            Data = notification.Data,
            Priority = notification.Priority,
            Timestamp = notification.CreatedAt,
            IsRead = notification.IsRead
        };

        await _notificationHub.Clients.User(notification.UserId.ToString())
            .SendAsync("ReceiveNotification", notificationData);
    }

    private async Task SaveAndSendBulkNotifications(List<Notification> notifications, CancellationToken cancellationToken)
    {
        if (!notifications.Any()) return;

        // Save to database
        await _notificationRepository.AddRangeAsync(notifications);
        await _notificationRepository.SaveChangesAsync(cancellationToken);

        // Send real-time notifications
        var tasks = notifications.Select(async notification =>
        {
            var notificationData = new
            {
                Id = notification.Id,
                Type = notification.Type.ToString(),
                Title = notification.Title,
                Message = notification.Message,
                Data = notification.Data,
                Priority = notification.Priority,
                Timestamp = notification.CreatedAt,
                IsRead = notification.IsRead
            };

            await _notificationHub.Clients.User(notification.UserId.ToString())
                .SendAsync("ReceiveNotification", notificationData);
        });

        await Task.WhenAll(tasks);
    }
}