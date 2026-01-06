using BugTrackr.Domain.Entities;

namespace BugTrackr.Application.Services.Email;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(User user);
    Task SendPasswordResetEmailAsync(User user, string resetToken);
    Task SendProjectInvitationEmailAsync(User user, Project project, User invitedBy);
    Task SendIssueAssignedEmailAsync(User assignee, Issue issue, User assignedBy);
    Task SendIssueCreatedEmailAsync(User user, Issue issue, User reporter);
    Task SendIssueUpdatedEmailAsync(User user, Issue issue, string updateType);
    Task SendCommentNotificationEmailAsync(User user, Issue issue, Comment comment);
    Task SendProjectUpdateEmailAsync(List<User> users, Project project, string updateType);
    Task SendWeeklyDigestEmailAsync(User user, List<Issue> assignedIssues, List<Project> projects);
    Task SendMentionNotificationEmailAsync(User user, Issue issue, Comment comment, User mentionedBy);
    Task SendBulkEmailAsync(List<string> recipients, string subject, string htmlContent, string textContent = null);
    Task SendChatMessageEmailAsync(User recipient, ChatMessage message, ChatRoom room);
    Task SendChatInvitationEmailAsync(User user, ChatRoom room, User invitedBy);
}
