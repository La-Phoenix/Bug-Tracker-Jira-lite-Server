using BugTrackr.Application.Services.Email;
using BugTrackr.Domain.Entities;
using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BugTrackr.Infrastructure.Services.Email;

public class MailjetEmailService : IEmailService
{
    private readonly MailjetClient _mailjetClient;
    private readonly MailJetSettings _settings;
    private readonly ILogger<MailjetEmailService> _logger;

    public MailjetEmailService(IOptions<MailJetSettings> settings, ILogger<MailjetEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _mailjetClient = new MailjetClient(_settings.ApiKey, _settings.ApiSecret);
    }

    public async Task SendWelcomeEmailAsync(User user)
    {
        try
        {
            var (subject, html, text) = EmailTemplates.WelcomeEmail(user);
            await SendEmailAsync(user.Email, user.Name, subject, html, text);
            _logger.LogInformation("Welcome email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
        }
    }

    public async Task SendIssueCreatedEmailAsync(User user, Issue issue, User reporter)
    {
        try
        {
            if (!user.IssueUpdates) return;

            var (subject, html, text) = EmailTemplates.IssueCreatedEmail(user, issue, reporter);
            await SendEmailAsync(user.Email, user.Name, subject, html, text);
            _logger.LogInformation("Issue creation email sent to {Email} for issue {IssueId}", user.Email, issue.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send issue creation email to {Email}", user.Email);
        }
    }

    public async Task SendIssueAssignedEmailAsync(User assignee, Issue issue, User assignedBy)
    {
        try
        {
            if (!assignee.AssignmentNotifications) return;

            var (subject, html, text) = EmailTemplates.IssueAssignedEmail(assignee, issue, assignedBy);
            await SendEmailAsync(assignee.Email, assignee.Name, subject, html, text);
            _logger.LogInformation("Issue assigned email sent to {Email} for issue {IssueId}", assignee.Email, issue.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send issue assigned email to {Email}", assignee.Email);
        }
    }

    public async Task SendCommentNotificationEmailAsync(User user, Issue issue, Comment comment)
    {
        try
        {
            if (!user.CommentNotifications) return;

            var (subject, html, text) = EmailTemplates.CommentNotificationEmail(user, issue, comment);
            await SendEmailAsync(user.Email, user.Name, subject, html, text);
            _logger.LogInformation("Comment notification email sent to {Email} for issue {IssueId}", user.Email, issue.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send comment notification email to {Email}", user.Email);
        }
    }

    public async Task SendPasswordResetEmailAsync(User user, string resetToken)
    {
        try
        {
            var (subject, html, text) = EmailTemplates.PasswordResetEmail(user, resetToken);

            await SendEmailAsync(user.Email, user.Name, subject, html, text);
            _logger.LogInformation("Password reset email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
        }
    }

    public async Task SendProjectInvitationEmailAsync(User user, Project project, User invitedBy)
    {
        try
        {
            var (subject, html) = EmailTemplates.ProjectInvitationEmail(project, user, invitedBy);

            await SendEmailAsync(user.Email, user.Name, subject, html);
            _logger.LogInformation("Project invitation email sent to {Email} for project {ProjectId}", user.Email, project.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send project invitation email to {Email}", user.Email);
        }
    }

    public async Task SendIssueUpdatedEmailAsync(User user, Issue issue, string updateType)
    {
        try
        {
            if (!user.IssueUpdates) return;

            var subject = $"Issue Updated: {issue.Title}";
            await SendEmailAsync(user.Email, user.Name, subject, $"Issue {issue.Title} has been {updateType}.");
            _logger.LogInformation("Issue update email sent to {Email} for issue {IssueId}", user.Email, issue.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send issue update email to {Email}", user.Email);
        }
    }

    public async Task SendProjectUpdateEmailAsync(List<User> users, Project project, string updateType)
    {
        try
        {
            var recipients = users.Where(u => u.ProjectUpdates).ToList();
            if (!recipients.Any()) return;

            var subject = $"Project Update: {project.Name}";
            var emails = recipients.Select(u => u.Email).ToList();

            await SendBulkEmailAsync(emails, subject, $"Project {project.Name} has been {updateType}.");
            _logger.LogInformation("Project update emails sent to {Count} recipients for project {ProjectId}", recipients.Count, project.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send project update emails for project {ProjectId}", project.Id);
        }
    }

    public async Task SendWeeklyDigestEmailAsync(User user, List<Issue> assignedIssues, List<Project> projects)
    {
        try
        {
            if (!user.WeeklyDigest) return;

            var (subject, html, text) = EmailTemplates.WeeklyDigestEmail(user, assignedIssues, projects);
            await SendEmailAsync(user.Email, user.Name, subject, html, text);
            _logger.LogInformation("Weekly digest sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send weekly digest to {Email}", user.Email);
        }
    }

    public async Task SendMentionNotificationEmailAsync(User user, Issue issue, Comment comment, User mentionedBy)
    {
        try
        {
            if (!user.MentionAlerts) return;

            var (subject, html, text) = EmailTemplates.MentionNotificationEmail(user, issue, comment, mentionedBy);
            await SendEmailAsync(user.Email, user.Name, subject, html, text);
            _logger.LogInformation("Mention notification sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send mention notification to {Email}", user.Email);
        }
    }

    public async Task SendBulkEmailAsync(List<string> recipients, string subject, string htmlContent, string textContent = null)
    {
        try
        {
            var email = new TransactionalEmailBuilder()
                .WithFrom(new SendContact(_settings.SenderEmail, _settings.SenderName))
                .WithSubject(subject)
                .WithHtmlPart(htmlContent);

            if (!string.IsNullOrEmpty(textContent))
                email.WithTextPart(textContent);

            foreach (var recipient in recipients)
            {
                email.WithTo(new SendContact(recipient));
            }

            var response = await _mailjetClient.SendTransactionalEmailAsync(email.Build());
            _logger.LogInformation("Bulk email sent to {Count} recipients", recipients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk email to {Count} recipients", recipients.Count);
            throw;
        }
    }

    private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlContent, string textContent = null)
    {
        var email = new TransactionalEmailBuilder()
            .WithFrom(new SendContact(_settings.SenderEmail, _settings.SenderName))
            .WithTo(new SendContact(toEmail, toName))
            .WithSubject(subject)
            .WithHtmlPart(htmlContent);

        if (!string.IsNullOrEmpty(textContent))
            email.WithTextPart(textContent);

        var response = await _mailjetClient.SendTransactionalEmailAsync(email.Build());

        if (response.Messages?.Length > 0)
        {
            var message = response.Messages[0];
            if (message.Status != "success")
            {
                throw new Exception($"Failed to send email: {message.Status}");
            }
        }
    }
}
