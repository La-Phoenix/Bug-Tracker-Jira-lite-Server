using BugTrackr.Domain.Entities;

namespace BugTrackr.Application.Services.NotificationService;

public interface INotificationService
{
    Task SendIssueAssignedNotification(User assignee, Issue issue, User assignedB, CancellationToken cancellationToken);
    Task SendCommentNotification(List<User> recipients, Issue issue, Comment comment, CancellationToken cancellationToken);
    Task SendMentionNotification(User user, Issue issue, Comment comment, User mentionedBy, CancellationToken cancellationToken);
    Task SendProjectInvitationNotification(User user, Project project, User invitedBy, CancellationToken cancellationToken);
    Task SendIssueCreatedNotification(List<User> projectMembers, Issue issue, User reporter, CancellationToken cancellationToken);
    Task SendIssueUpdatedNotification(List<User> watchers, Issue issue, string updateType, User updatedBy, CancellationToken cancellationToken);
    Task SendChatMessageNotification(List<User> recipients, ChatMessage message, ChatRoom room, CancellationToken cancellationToken);
    Task SendChatInvitationNotification(User user, ChatRoom room, User invitedBy, CancellationToken cancellationToken);

    // Notification management
    Task<List<Notification>> GetUserNotifications(int userId, bool unreadOnly = false, int limit = 50);
    Task MarkNotificationAsRead(int notificationId, int userId, CancellationToken cancellationToken);
    Task MarkAllNotificationsAsRead(int userId, CancellationToken cancellationToken);
    Task DeleteNotification(int notificationId, int userId, CancellationToken cancellationToken);
    Task CleanupExpiredNotifications(CancellationToken cancellationToken);
}