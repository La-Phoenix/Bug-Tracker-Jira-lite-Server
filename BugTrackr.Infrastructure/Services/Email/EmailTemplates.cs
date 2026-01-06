using BugTrackr.Domain.Entities;
using BugTrackr.Domain.Enums;
using System.Linq;

namespace BugTrackr.Infrastructure.Services.Email;

public static class EmailTemplates
{
    public static (string subject, string html, string text) WelcomeEmail(User user)
    {
        var subject = "Welcome to BugTrackr!";
        var html = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background: #4f46e5; color: white; padding: 20px; text-align: center; }}
                .content {{ padding: 30px; background: #f8fafc; }}
                .button {{ display: inline-block; background: #4f46e5; color: white; padding: 12px 24px; 
                           text-decoration: none; border-radius: 5px; margin: 20px 0; }}
            </style>
        </head>
        <body>
            <div class=""container"">
                <div class=""header"">
                    <h1>Welcome to BugTrackr</h1>
                </div>
                <div class=""content"">
                    <h2>Hello {user.Name}!</h2>
                    <p>Welcome to BugTrackr - your comprehensive bug tracking and project management solution.</p>
                    <p>You've been automatically added to the Sample Project to get you started. You can now:</p>
                    <ul>
                        <li>Create and track issues</li>
                        <li>Collaborate with team members</li>
                        <li>Manage project workflows</li>
                        <li>Stay organized with real-time updates</li>
                    </ul>
                    <a href=""https://bug-tracker-jira-lite-client.vercel.app"" class=""button"">Get Started</a>
                    <p>If you have any questions, feel free to reach out to our support team.</p>
                    <p>Best regards,<br>The BugTrackr Team</p>
                </div>
            </div>
        </body>
        </html>";

        var text = $@"Welcome to BugTrackr!

            Hello {user.Name}!

            Welcome to BugTrackr - your comprehensive bug tracking and project management solution.

            You've been automatically added to the Sample Project to get you started. You can now:
            - Create and track issues
            - Collaborate with team members  
            - Manage project workflows
            - Stay organized with real-time updates

            Get started at: https://bug-tracker-jira-lite-client.vercel.app

            If you have any questions, feel free to reach out to our support team.

            Best regards,
            The BugTrackr Team";

        return (subject, html, text);
    }

    //public static (string subject, string html, string text) PasswordResetEmail(User user, string resetToken)
    //{
    //    var resetLink = $"https://bug-tracker-jira-lite-client.vercel.app/reset-password?token={resetToken}";
    //    var subject = "Password Reset Request";
    //    var html = $@"
    //    <!DOCTYPE html>
    //    <html>
    //    <head>
    //        <style>
    //            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
    //            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
    //            .header {{ background: #ef4444; color: white; padding: 20px; text-align: center; }}
    //            .content {{ padding: 30px; background: #f8fafc; }}
    //            .button {{ display: inline-block; background: #ef4444; color: white; padding: 12px 24px; 
    //                       text-decoration: none; border-radius: 5px; margin: 20px 0; }}
    //        </style>
    //    </head>
    //    <body>
    //        <div class=""container"">
    //            <div class=""header"">
    //                <h1>Password Reset</h1>
    //            </div>
    //            <div class=""content"">
    //                <h2>Hello {user.Name}!</h2>
    //                <p>You requested a password reset for your BugTrackr account.</p>
    //                <a href=""{resetLink}"" class=""button"">Reset Password</a>
    //                <p>This link will expire in 1 hour. If you didn't request this reset, please ignore this email.</p>
    //                <p>Best regards,<br>The BugTrackr Team</p>
    //            </div>
    //        </div>
    //    </body>
    //    </html>";

    //    var text = $@"Password Reset

    //    Hello {user.Name}!

    //    You requested a password reset for your BugTrackr account.

    //    Reset your password at: {resetLink}

    //    This link will expire in 1 hour. If you didn't request this reset, please ignore this email.

    //    Best regards,
    //    The BugTrackr Team";
    //    return (subject, html, text);
    //}

    public static (string subject, string html, string text) IssueAssignedEmail(User assignee, Issue issue, User assignedBy)
    {
        var subject = $"Issue Assigned: {issue.Title}";

        var validLabels = issue.IssueLabels?.Where(il => il.Label != null).ToList() ?? new List<IssueLabel>();
        var labelNames = validLabels.Select(il => il.Label.Name).ToList();
        var labelsString = labelNames.Any() ? string.Join(", ", labelNames) : "None";
        var labelsHtml = string.Join(
        "",
        validLabels
            .Select(il =>
                $@"<span class=""label-tag""
                    style=""display:inline-block;
                           margin:2px;
                           padding:4px 8px;
                           border-radius:4px;
                           background-color:{il.Label.Color};
                           color:white;"">
                    {il.Label.Name}
                </span>"
            )
        );

        var html = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background: #10b981; color: white; padding: 20px; text-align: center; }}
                .content {{ padding: 30px; background: #f8fafc; }}
                .issue-details {{ background: white; padding: 20px; border-radius: 5px; margin: 20px 0; }}
                .button {{ display: inline-block; background: #10b981; color: white; padding: 12px 24px; 
                            text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                .priority-{issue.Priority?.Name?.ToLower() ?? "medium"} {{ color: {GetPriorityColor(issue.Priority?.Name ?? "Medium")}; font-weight: bold; }}
                .labels {{ margin-top: 10px; }}
                .label-tag {{ display: inline-block; background: #e5e7eb; color: #374151; padding: 2px 8px; 
                                border-radius: 12px; font-size: 0.8em; margin-right: 5px; }}
            </style>
        </head>
        <body>
            <div class=""container"">
                <div class=""header"">
                    <h1>New Issue Assigned</h1>
                </div>
                <div class=""content"">
                    <h2>Hello {assignee.Name}!</h2>
                    <p>You have been assigned a new issue by {assignedBy.Name}.</p>

                    <div class=""issue-details"">
                        <h3>{issue.Title}</h3>
                        <p><strong>Description:</strong> {issue.Description}</p>
                        <p><strong>Priority:</strong> <span class=""priority-{issue.Priority?.Name?.ToLower() ?? "medium"}"">{issue.Priority?.Name ?? "Medium"}</span></p>
                        <p><strong>Status:</strong> {issue.Status?.Name ?? "Open"}</p>
                        <p><strong>Due Date:</strong> {(issue.DueDate?.ToString("MMM dd, yyyy") ?? "Not set")}</p>
                
                        {(validLabels.Any() ? $@"
                        <div class=""labels"">
                            <strong>🏷️ Labels:</strong><br>
                            {labelsHtml}
                        </div>" : "")}
                    </div>

                    <a href=""https://bug-tracker-jira-lite-client.vercel.app/issues/{issue.Id}"" class=""button"">View Issue</a>

                    <p>Best regards,<br>The BugTrackr Team</p>
                </div>
            </div>
        </body>
        </html>";

        var text = $@"New Issue Assigned

        Hello {assignee.Name}!

        You have been assigned a new issue by {assignedBy.Name}.

        Issue Details:
        - Title: {issue.Title}
        - Description: {issue.Description}
        - Priority: {issue.Priority?.Name ?? "Medium"}
        - Status: {issue.Status?.Name ?? "Open"}
        - Labels: {labelsString}
        - Due Date: {(issue.DueDate?.ToString("MMM dd, yyyy") ?? "Not set")}

        View issue at: https://bug-tracker-jira-lite-client.vercel.app/issues/{issue.Id}

        Best regards,
        The BugTrackr Team";

        return (subject, html, text);
    }


    public static (string subject, string html, string text) CommentNotificationEmail(User user, Issue issue, Comment comment)
    {
        var subject = $"New Comment on Issue: {issue.Title}";
        var html = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background: #8b5cf6; color: white; padding: 20px; text-align: center; }}
                .content {{ padding: 30px; background: #f8fafc; }}
                .comment-box {{ background: white; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #8b5cf6; }}
                .button {{ display: inline-block; background: #8b5cf6; color: white; padding: 12px 24px; 
                           text-decoration: none; border-radius: 5px; margin: 20px 0; }}
            </style>
        </head>
        <body>
            <div class=""container"">
                <div class=""header"">
                    <h1>New Comment</h1>
                </div>
                <div class=""content"">
                    <h2>Hello {user.Name}!</h2>
                    <p>A new comment has been added to issue <strong>{issue.Title}</strong>.</p>
            
                    <div class=""comment-box"">
                        <p><strong>Comment by:</strong> {comment.Author.Name}</p>
                        <p><strong>Date:</strong> {comment.CreatedAt:MMM dd, yyyy 'at' hh:mm tt}</p>
                        <p><strong>Comment:</strong></p>
                        <p>{comment.Content}</p>
                    </div>
            
                    <a href=""https://bug-tracker-jira-lite-client.vercel.app/issues/{issue.Id}"" class=""button"">View Issue</a>
            
                    <p>Best regards,<br>The BugTrackr Team</p>
                </div>
            </div>
        </body>
        </html>";

        var text = $@"New Comment

        Hello {user.Name}!

        A new comment has been added to issue ""{issue.Title}"".

        Comment by: {comment.Author.Name}
        Date: {comment.CreatedAt:MMM dd, yyyy 'at' hh:mm tt}
        Comment: {comment.Content}

        View issue at: https://bug-tracker-jira-lite-client.vercel.app/issues/{issue.Id}

        Best regards,
        The BugTrackr Team";

        return (subject, html, text);
    }

    public static (string subject, string html, string text) PasswordResetEmail(User user, string resetToken)
    {
        var resetLink = $"https://bug-tracker-jira-lite-client.vercel.app/reset-password?token={resetToken}";
        var subject = "Password Reset Request";
        var html = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background: #ef4444; color: white; padding: 20px; text-align: center; }}
                .content {{ padding: 30px; background: #f8fafc; }}
                .button {{ display: inline-block; background: #ef4444; color: white; padding: 12px 24px; 
                           text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                .warning {{ background: #fef2f2; border-left: 4px solid #ef4444; padding: 15px; margin: 20px 0; }}
            </style>
        </head>
        <body>
            <div class=""container"">
                <div class=""header"">
                    <h1>Password Reset Request</h1>
                </div>
                <div class=""content"">
                    <h2>Hello {user.Name}!</h2>
                    <p>You requested a password reset for your BugTrackr account.</p>
            
                    <div class=""warning"">
                        <p><strong>Security Notice:</strong> If you didn't request this password reset, please ignore this email and your password will remain unchanged.</p>
                    </div>
            
                    <p>To reset your password, click the button below:</p>
                    <a href=""{resetLink}"" class=""button"">Reset Password</a>
            
                    <p>Or copy and paste this link into your browser:</p>
                    <p><a href=""{resetLink}"">{resetLink}</a></p>
            
                    <p><strong>This link will expire in 1 hour for security reasons.</strong></p>
            
                    <p>Best regards,<br>The BugTrackr Team</p>
                </div>
            </div>
        </body>
        </html>";

        var text = $@"Password Reset Request

        Hello {user.Name}!

        You requested a password reset for your BugTrackr account.

        SECURITY NOTICE: If you didn't request this password reset, please ignore this email and your password will remain unchanged.

        To reset your password, visit: {resetLink}

        This link will expire in 1 hour for security reasons.

        Best regards,
        The BugTrackr Team";

        return (subject, html, text);
    }

    public static (string subject, string html) ProjectInvitationEmail(Project project, User user, User invitedBy)
    {
        var subject = $"You've been invited to join '{project.Name}'";
        var html = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background: #06b6d4; color: white; padding: 20px; text-align: center; }}
                .content {{ padding: 30px; background: #f8fafc; }}
                .project-info {{ background: white; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #06b6d4; }}
                .button {{ display: inline-block; background: #06b6d4; color: white; padding: 12px 24px; 
                           text-decoration: none; border-radius: 5px; margin: 20px 0; }}
            </style>
        </head>
        <body>
            <div class=""container"">
                <div class=""header"">
                    <h1>Project Invitation</h1>
                </div>
                <div class=""content"">
                    <h2>Hello {user.Name}!</h2>
                    <p><strong>{invitedBy.Name}</strong> has invited you to collaborate on a project in BugTrackr.</p>
            
                    <div class=""project-info"">
                        <h3>📋 {project.Name}</h3>
                        <p><strong>Description:</strong> {project.Description ?? "No description provided"}</p>
                        <p><strong>Project Owner:</strong> {invitedBy.Name}</p>
                        <p><strong>Invited on:</strong> {DateTime.UtcNow:MMM dd, yyyy}</p>
                    </div>
            
                    <p>As a project member, you'll be able to:</p>
                    <ul>
                        <li>🐛 View and manage issues</li>
                        <li>💬 Comment and collaborate with team members</li>
                        <li>📈 Track project progress</li>
                        <li>🔔 Receive notifications about project updates</li>
                    </ul>
            
                    <a href=""https://bug-tracker-jira-lite-client.vercel.app/projects/{project.Id}"" class=""button"">View Project</a>
            
                    <p>Welcome to the team!</p>
                    <p>Best regards,<br>The BugTrackr Team</p>
                </div>
            </div>
        </body>
        </html>";

        return (subject, html);
    }

    public static (string subject, string html, string text) MentionNotificationEmail(User user, Issue issue, Comment comment, User mentionedBy)
    {
        var subject = $"You were mentioned in '{issue.Title}'";
        var html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: #f59e0b; color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 30px; background: #f8fafc; }}
                    .mention-box {{ background: white; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #f59e0b; }}
                    .comment-box {{ background: #fffbeb; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                    .button {{ display: inline-block; background: #f59e0b; color: white; padding: 12px 24px; 
                               text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                </style>
            </head>
            <body>
                <div class=""container"">
                    <div class=""header"">
                        <h1>👋 You were mentioned!</h1>
                    </div>
                    <div class=""content"">
                        <h2>Hello {user.Name}!</h2>
                        <p><strong>{mentionedBy.Name}</strong> mentioned you in a comment on issue <strong>'{issue.Title}'</strong>.</p>
            
                        <div class=""mention-box"">
                            <h3>📋 Issue: {issue.Title}</h3>
                            <p><strong>Description:</strong> {issue.Description}</p>
                            <p><strong>Status:</strong> {issue.Status?.Name ?? "Unknown"}</p>
                            <p><strong>Priority:</strong> {issue.Priority?.Name ?? "Unknown"}</p>
                        </div>
            
                        <div class=""comment-box"">
                            <p><strong>💬 {mentionedBy.Name} commented:</strong></p>
                            <p>{comment.Content}</p>
                            <p><small>📅 {comment.CreatedAt:MMM dd, yyyy 'at' hh:mm tt}</small></p>
                        </div>
            
                        <a href=""https://bug-tracker-jira-lite-client.vercel.app/issues/{issue.Id}"" class=""button"">View Issue & Respond</a>
            
                        <p>Stay connected with your team!</p>
                        <p>Best regards,<br>The BugTrackr Team</p>
                    </div>
                </div>
            </body>
            </html>";

                    var text = $@"You were mentioned!

            Hello {user.Name}!

            {mentionedBy.Name} mentioned you in a comment on issue '{issue.Title}'.

            Issue Details:
            - Title: {issue.Title}
            - Description: {issue.Description}
            - Status: {issue.Status?.Name ?? "Unknown"}
            - Priority: {issue.Priority?.Name ?? "Unknown"}

            Comment by {mentionedBy.Name}:
            {comment.Content}

            View issue and respond at: https://bug-tracker-jira-lite-client.vercel.app/issues/{issue.Id}

            Best regards,
            The BugTrackr Team";

        return (subject, html, text);
    }

    public static (string subject, string html, string text) IssueCreatedEmail(User user, Issue issue, User reporter)
    {
        var priorityColor = GetPriorityColor(issue.Priority?.Name ?? "Medium");
        var subject = $"New Issue Created: {issue.Title}";

        var validLabels = issue.IssueLabels?.Where(il => il.Label != null).ToList() ?? new List<IssueLabel>();
        var labelNames = validLabels.Select(il => il.Label.Name).ToList();
        var labelsString = labelNames.Any() ? string.Join(", ", labelNames) : "None";
        var labelsHtml = string.Join(
        "",
        validLabels
            .Select(il =>
                $@"<span class=""label-tag""
                    style=""display:inline-block;
                           margin:2px;
                           padding:4px 8px;
                           border-radius:4px;
                           background-color:{il.Label.Color};
                           color:white;"">
                    {il.Label.Name}
                </span>"
            )
        );

        var html = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background: #3b82f6; color: white; padding: 20px; text-align: center; }}
                .content {{ padding: 30px; background: #f8fafc; }}
                .issue-details {{ background: white; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #3b82f6; }}
                .button {{ display: inline-block; background: #3b82f6; color: white; padding: 12px 24px; 
                           text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                .priority {{ color: {priorityColor}; font-weight: bold; }}
                .meta-info {{ background: #f1f5f9; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                .status-badge {{ display: inline-block; background: #10b981; color: white; padding: 4px 8px; 
                                border-radius: 12px; font-size: 0.8em; }}
                .high-priority {{ background: #fef2f2; border-left: 4px solid #ef4444; }}
                .labels {{ margin-top: 10px; }}
                .label-tag {{ display: inline-block; background: #e5e7eb; color: #374151; padding: 2px 8px; 
                             border-radius: 12px; font-size: 0.8em; margin-right: 5px; }}
            </style>
        </head>
        <body>
            <div class=""container"">
                <div class=""header"">
                    <h1>🆕 New Issue Created</h1>
                </div>
                <div class=""content"">
                    <h2>Hello {user.Name}!</h2>
                    <p>A new issue has been created in project <strong>{issue.Project?.Name}</strong> by <strong>{reporter.Name}</strong>.</p>
            
                    <div class=""issue-details {(issue.Priority?.Name == "High" || issue.Priority?.Name == "Critical" ? "high-priority" : "")}"">
                        <h3>📋 {issue.Title}</h3>
                        <p><strong>Description:</strong></p>
                        <p>{issue.Description ?? "No description provided"}</p>
                
                        <div class=""meta-info"">
                            <p><strong>🎯 Priority:</strong> <span class=""priority"">{issue.Priority?.Name ?? "Medium"}</span></p>
                            <p><strong>📊 Status:</strong> <span class=""status-badge"">{issue.Status?.Name ?? "Open"}</span></p>
                            <p><strong>👤 Reported by:</strong> {reporter.Name} ({reporter.Email})</p>
                            {(issue.AssigneeId.HasValue ? $"<p><strong>👥 Assigned to:</strong> {issue.Assignee?.Name}</p>" : "<p><strong>👥 Assigned to:</strong> Unassigned</p>")}
                            <p><strong>📅 Created:</strong> {issue.CreatedAt:MMM dd, yyyy 'at' hh:mm tt}</p>
                            {(issue.DueDate.HasValue ? $"<p><strong>⏰ Due Date:</strong> {issue.DueDate.Value:MMM dd, yyyy}</p>" : "")}
                        </div>

                         {(validLabels.Any() ? $@"
                        <div class=""labels"">
                            <strong>🏷️ Labels:</strong><br>
                            {labelsHtml}
                        </div>" : "")}
                    </div>

            
                    {(issue.Priority?.Name == "High" || issue.Priority?.Name == "Critical" ?
                            "<p style='color: #ef4444; font-weight: bold;'>⚠️ This is a high priority issue that requires immediate attention!</p>" : "")}
            
                    <a href=""https://bug-tracker-jira-lite-client.vercel.app/issues/{issue.Id}"" class=""button"">View Issue Details</a>
            
                    <p>Stay updated with your project progress!</p>
                    <p>Best regards,<br>The BugTrackr Team</p>
                </div>
            </div>
        </body>
        </html>";

        var text = $@"New Issue Created

        Hello {user.Name}!

        A new issue has been created in project ""{issue.Project?.Name}"" by {reporter.Name}.

        Issue Details:
        - Title: {issue.Title}
        - Description: {issue.Description ?? "No description provided"}
        - Priority: {issue.Priority?.Name ?? "Medium"}
        - Status: {issue.Status?.Name ?? "Open"}
        - Labels: {labelsString}
        - Reported by: {reporter.Name} ({reporter.Email})
        {(issue.AssigneeId.HasValue ? $"- Assigned to: {issue.Assignee?.Name}" : "- Assigned to: Unassigned")}
        - Created: {issue.CreatedAt:MMM dd, yyyy 'at' hh:mm tt}
        {(issue.DueDate.HasValue ? $"- Due Date: {issue.DueDate.Value:MMM dd, yyyy}" : "")}

        {(issue.Priority?.Name == "High" || issue.Priority?.Name == "Critical" ?
                "⚠️ This is a high priority issue that requires immediate attention!" : "")}

        View issue at: https://bug-tracker-jira-lite-client.vercel.app/issues/{issue.Id}

        Best regards,
        The BugTrackr Team";

        return (subject, html, text);
    }


    public static (string subject, string html, string text) WeeklyDigestEmail(User user, List<Issue> assignedIssues, List<Project> projects)
    {
        var subject = "Your Weekly BugTrackr Digest";
        var overdueTasks = assignedIssues.Where(i => i.DueDate.HasValue && i.DueDate.Value < DateTime.UtcNow).ToList();
        var upcomingTasks = assignedIssues.Where(i => i.DueDate.HasValue && i.DueDate.Value > DateTime.UtcNow && i.DueDate.Value <= DateTime.UtcNow.AddDays(7)).ToList();

        var html = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ background: #6366f1; color: white; padding: 20px; text-align: center; }}
                .content {{ padding: 30px; background: #f8fafc; }}
                .section {{ background: white; padding: 20px; border-radius: 5px; margin: 20px 0; }}
                .stats {{ display: flex; justify-content: space-around; margin: 20px 0; }}
                .stat-item {{ text-align: center; }}
                .stat-number {{ font-size: 2em; font-weight: bold; color: #6366f1; }}
                .urgent {{ color: #ef4444; font-weight: bold; }}
                .button {{ display: inline-block; background: #6366f1; color: white; padding: 12px 24px; 
                           text-decoration: none; border-radius: 5px; margin: 20px 0; }}
            </style>
        </head>
        <body>
            <div class=""container"">
                <div class=""header"">
                    <h1>📊 Your Weekly Digest</h1>
                    <p>Week of {DateTime.UtcNow.AddDays(-7):MMM dd} - {DateTime.UtcNow:MMM dd, yyyy}</p>
                </div>
                <div class=""content"">
                    <h2>Hello {user.Name}!</h2>
                    <p>Here's your weekly summary of activity and tasks in BugTrackr.</p>
            
                    <div class=""section"">
                        <h3>📈 Your Stats</h3>
                        <div class=""stats"">
                            <div class=""stat-item"">
                                <div class=""stat-number"">{assignedIssues.Count}</div>
                                <div>Assigned Issues</div>
                            </div>
                            <div class=""stat-item"">
                                <div class=""stat-number"">{projects.Count}</div>
                                <div>Active Projects</div>
                            </div>
                            <div class=""stat-item"">
                                <div class=""stat-number"">{overdueTasks.Count}</div>
                                <div>Overdue Tasks</div>
                            </div>
                        </div>
                    </div>
            
                    {(overdueTasks.Any() ? $@"
                    <div class=""section"">
                        <h3>🚨 Overdue Tasks ({overdueTasks.Count})</h3>
                        {string.Join("", overdueTasks.Take(5).Select(task => $@"
                        <p class=""urgent"">• {task.Title} (Due: {task.DueDate?.ToString("MMM dd")})</p>"))}
                        {(overdueTasks.Count > 5 ? $"<p>...and {overdueTasks.Count - 5} more</p>" : "")}
                    </div>" : "")}
            
                    {(upcomingTasks.Any() ? $@"
                    <div class=""section"">
                        <h3>📅 Due This Week ({upcomingTasks.Count})</h3>
                        {string.Join("", upcomingTasks.Take(5).Select(task => $@"
                        <p>• {task.Title} (Due: {task.DueDate?.ToString("MMM dd")})</p>"))}
                        {(upcomingTasks.Count > 5 ? $"<p>...and {upcomingTasks.Count - 5} more</p>" : "")}
                    </div>" : "")}
            
                    <a href=""https://bug-tracker-jira-lite-client.vercel.app/dashboard"" class=""button"">View Dashboard</a>
            
                    <p>Keep up the great work!</p>
                    <p>Best regards,<br>The BugTrackr Team</p>
                </div>
            </div>
        </body>
        </html>";

        var text = $@"Your Weekly BugTrackr Digest
        Week of {DateTime.UtcNow.AddDays(-7):MMM dd} - {DateTime.UtcNow:MMM dd, yyyy}

        Hello {user.Name}!

        Here's your weekly summary:

        📊 Your Stats:
        - {assignedIssues.Count} Assigned Issues
        - {projects.Count} Active Projects
        - {overdueTasks.Count} Overdue Tasks

        {(overdueTasks.Any() ? $@"
        🚨 Overdue Tasks:
        {string.Join("\n", overdueTasks.Take(5).Select(t => $"• {t.Title} (Due: {t.DueDate?.ToString("MMM dd")})"))}
        {(overdueTasks.Count > 5 ? $"...and {overdueTasks.Count - 5} more" : "")}" : "")}

        {(upcomingTasks.Any() ? $@"
        📅 Due This Week:
        {string.Join("\n", upcomingTasks.Take(5).Select(t => $"• {t.Title} (Due: {t.DueDate?.ToString("MMM dd")})"))}
        {(upcomingTasks.Count > 5 ? $"...and {upcomingTasks.Count - 5} more" : "")}" : "")}

        View your dashboard: https://bug-tracker-jira-lite-client.vercel.app/dashboard

        Keep up the great work!
        The BugTrackr Team";

        return (subject, html, text);
    }

    public static (string subject, string html, string text) ChatMessageEmail(User recipient, ChatMessage message, ChatRoom room)
    {
        var subject = room.Type == ChatRoomType.Direct
            ? $"New message from {message.Sender.Name}"
            : $"New message in {room.Name}";

        var messagePreview = message.Content.Length > 100
            ? message.Content[..100] + "..."
            : message.Content;

        var html = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>{subject}</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; }}
                .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; }}
                .header {{ background-color: #2563eb; color: white; padding: 20px; text-align: center; }}
                .content {{ padding: 30px; }}
                .message-container {{ background-color: #f8fafc; border-left: 4px solid #2563eb; padding: 15px; margin: 20px 0; }}
                .sender {{ font-weight: bold; color: #1e40af; margin-bottom: 5px; }}
                .message-content {{ color: #374151; line-height: 1.6; }}
                .room-info {{ background-color: #eff6ff; padding: 10px; border-radius: 5px; margin: 15px 0; }}
                .footer {{ background-color: #f9fafb; padding: 20px; text-align: center; font-size: 12px; color: #6b7280; }}
                .button {{ display: inline-block; background-color: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 15px 0; }}
                .timestamp {{ color: #6b7280; font-size: 12px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>💬 {subject}</h1>
                </div>
                <div class='content'>
                    <h2>Hello {recipient.Name},</h2>
                    
                    {(room.Type == ChatRoomType.Direct ?
                        $"<p>You have a new direct message from <strong>{message.Sender.Name}</strong>:</p>" :
                        $"<p>There's a new message in <strong>{room.Name}</strong>:</p>")}
                    
                    <div class='message-container'>
                        <div class='sender'>{message.Sender.Name}</div>
                        <div class='message-content'>{messagePreview}</div>
                        <div class='timestamp'>{message.CreatedAt:MMM dd, yyyy 'at' h:mm tt}</div>
                    </div>
                    
                    {(room.Type != ChatRoomType.Direct ? $@"
                    <div class='room-info'>
                        <strong>Chat Room:</strong> {room.Name}<br>
                        {(!string.IsNullOrEmpty(room.Description) ? $"<strong>Description:</strong> {room.Description}" : "")}
                    </div>" : "")}
                    
                    <p>
                        <a href='#' class='button'>View in BugTrackr</a>
                    </p>
                    
                    <p>Stay connected with your team and never miss important conversations.</p>
                </div>
                <div class='footer'>
                    <p>This email was sent by BugTrackr. You can manage your notification preferences in your account settings.</p>
                </div>
            </div>
        </body>
        </html>";

        var text = $@"
        {subject}
        
        Hello {recipient.Name},
        
        {(room.Type == ChatRoomType.Direct ?
            $"You have a new direct message from {message.Sender.Name}:" :
            $"There's a new message in {room.Name}:")}
        
        From: {message.Sender.Name}
        Message: {messagePreview}
        Time: {message.CreatedAt:MMM dd, yyyy 'at' h:mm tt}
        
        {(room.Type != ChatRoomType.Direct ? $"Chat Room: {room.Name}" : "")}
        
        Log in to BugTrackr to view the full conversation and reply.
        
        ---
        This email was sent by BugTrackr. You can manage your notification preferences in your account settings.
        ";

        return (subject, html, text);
    }

    public static (string subject, string html, string text) ChatInvitationEmail(User user, ChatRoom room, User invitedBy)
    {
        var subject = room.Type == ChatRoomType.Direct
            ? $"{invitedBy.Name} started a chat with you"
            : $"You've been added to {room.Name}";

        var html = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>{subject}</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; }}
                .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; }}
                .header {{ background-color: #059669; color: white; padding: 20px; text-align: center; }}
                .content {{ padding: 30px; }}
                .invitation-box {{ background-color: #ecfdf5; border: 2px solid #059669; padding: 20px; border-radius: 8px; margin: 20px 0; text-align: center; }}
                .room-details {{ background-color: #f8fafc; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                .footer {{ background-color: #f9fafb; padding: 20px; text-align: center; font-size: 12px; color: #6b7280; }}
                .button {{ display: inline-block; background-color: #059669; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 15px 0; }}
                .invited-by {{ color: #1f2937; font-weight: bold; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>🎉 {subject}</h1>
                </div>
                <div class='content'>
                    <h2>Hello {user.Name}!</h2>
                    
                    <div class='invitation-box'>
                        <h3>You've been invited to join a chat!</h3>
                        <p class='invited-by'>Invited by: {invitedBy.Name}</p>
                    </div>
                    
                    <div class='room-details'>
                        <h4>Chat Details:</h4>
                        <p><strong>Name:</strong> {room.Name}</p>
                        <p><strong>Type:</strong> {(room.Type == ChatRoomType.Direct ? "Direct Message" : "Group Chat")}</p>
                        {(!string.IsNullOrEmpty(room.Description) ? $"<p><strong>Description:</strong> {room.Description}</p>" : "")}
                        <p><strong>Created:</strong> {room.CreatedAt:MMM dd, yyyy 'at' h:mm tt}</p>
                    </div>
                    
                    <p>
                        <a href='#' class='button'>Join Chat Now</a>
                    </p>
                    
                    <p>Start collaborating with your team members and stay up-to-date on important discussions.</p>
                </div>
                <div class='footer'>
                    <p>This email was sent by BugTrackr. You can manage your notification preferences in your account settings.</p>
                </div>
            </div>
        </body>
        </html>";

        var text = $@"
        {subject}
        
        Hello {user.Name}!
        
        You've been invited to join a chat by {invitedBy.Name}.
        
        Chat Details:
        - Name: {room.Name}
        - Type: {(room.Type == ChatRoomType.Direct ? "Direct Message" : "Group Chat")}
        {(!string.IsNullOrEmpty(room.Description) ? $"- Description: {room.Description}" : "")}
        - Created: {room.CreatedAt:MMM dd, yyyy 'at' h:mm tt}
        
        Log in to BugTrackr to join the chat and start collaborating with your team.
        
        ---
        This email was sent by BugTrackr. You can manage your notification preferences in your account settings.
        ";

        return (subject, html, text);
    }

    private static string GetPriorityColor(string priority)
    {
        return priority.ToLower() switch
        {
            "high" => "#ef4444",
            "medium" => "#f59e0b",
            "low" => "#10b981",
            _ => "#6b7280"
        };
    }
}
