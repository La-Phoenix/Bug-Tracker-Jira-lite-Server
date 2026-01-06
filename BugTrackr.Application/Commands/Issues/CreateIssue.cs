using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Common.Helpers;
using BugTrackr.Application.DTOs.Issues;
using BugTrackr.Application.Services;
using BugTrackr.Application.Services.Email;
using BugTrackr.Application.Services.NotificationService;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Issues;

public record CreateIssueCommand(
    string Title,
    string? Description,
    int ReporterId,
    int? AssigneeId,
    int ProjectId,
    int StatusId,
    int PriorityId,
    List<int>? LabelIds = null
) : IRequest<ApiResponse<IssueDto>>, ISkipFluentValidation;

public class CreateIssueCommandValidator : AbstractValidator<CreateIssueCommand>
{
    public CreateIssueCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .MinimumLength(3);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.ReporterId)
            .GreaterThan(0);

        RuleFor(x => x.AssigneeId)
            .GreaterThan(0)
            .When(x => x.AssigneeId.HasValue);

        RuleFor(x => x.ProjectId)
            .GreaterThan(0);

        RuleFor(x => x.StatusId)
            .GreaterThan(0);

        RuleFor(x => x.PriorityId)
            .GreaterThan(0);
    }
}

public class CreateIssueCommandHandler : IRequestHandler<CreateIssueCommand, ApiResponse<IssueDto>>
{
    private readonly IRepository<Issue> _issueRepo;
    private readonly IRepository<Label> _labelRepo;
    private readonly IRepository<IssueLabel> _issueLabelRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateIssueCommandHandler> _logger;
    private readonly IValidator<CreateIssueCommand> _validator;

    public CreateIssueCommandHandler(
        IRepository<Issue> issueRepo,
        IRepository<Label> labelRepo,
        IRepository<IssueLabel> issueLabelRepo,
        IRepository<User> userRepo,
        IEmailService emailService,
        INotificationService notificationService,
        IMapper mapper,
        IValidator<CreateIssueCommand> validator,
        ILogger<CreateIssueCommandHandler> logger)
    {
        _issueRepo = issueRepo;
        _labelRepo = labelRepo;
        _issueLabelRepo = issueLabelRepo;
        _userRepo = userRepo;
        _emailService = emailService;
        _notificationService = notificationService;
        _mapper = mapper;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<IssueDto>> Handle(CreateIssueCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(err => err.ErrorMessage);
                var resp = ApiResponse<IssueDto>.Failure("Validation failed", 400);
                resp.Errors = errors;
                return resp;
            }


            // Create the issue
            var issue = new Issue
            {
                Title = request.Title,
                Description = request.Description,
                ProjectId = request.ProjectId,
                StatusId = request.StatusId,
                PriorityId = request.PriorityId,
                ReporterId = request.ReporterId,
                AssigneeId = request.AssigneeId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _issueRepo.AddAsync(issue);
            await _issueRepo.SaveChangesAsync(cancellationToken);

            // Create IssueLabel relationships if labels provided
            if (request.LabelIds != null && request.LabelIds.Any())
            {
                // Validate that all label IDs exist
                var existingLabelIds = await _labelRepo.Query()
                    .Where(l => request.LabelIds.Contains(l.Id))
                    .Select(l => l.Id)
                    .ToListAsync(cancellationToken);

                var validLabelIds = request.LabelIds.Where(id => existingLabelIds.Contains(id));

                // Create IssueLabel entries
                var issueLabels = validLabelIds.Select(labelId => new IssueLabel
                {
                    IssueId = issue.Id,
                    LabelId = labelId
                }).ToList();

                if (issueLabels.Any())
                {
                    await _issueLabelRepo.AddRangeAsync(issueLabels);
                    await _issueLabelRepo.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Added {Count} labels to issue {IssueId}",
                        issueLabels.Count, issue.Id);
                }
            }

            // Fetch the complete issue with all relationships
            var createdIssue = await _issueRepo.Query()
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .Include(i => i.Status)
                .Include(i => i.Priority)
                .Include(i => i.Project)
                .Include(i => i.IssueLabels)
                    .ThenInclude(il => il.Label)
                .FirstAsync(i => i.Id == issue.Id, cancellationToken);

            // Send email notifications
            if (createdIssue.Assignee != null) await _emailService.SendIssueCreatedEmailAsync(createdIssue.Assignee, createdIssue, createdIssue.Reporter);
            await SendCreationNotificationsAsync(createdIssue, cancellationToken);

            var issueDto = _mapper.Map<IssueDto>(createdIssue);

            return ApiResponse<IssueDto>.SuccessResponse(issueDto, 201, "Issue created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating issue: {Message}", ex.Message);
            return ApiResponse<IssueDto>.Failure("An unexpected error occurred.", 500);
        }
    }
    private async Task SendCreationNotificationsAsync(Issue issue, CancellationToken cancellationToken)
    {
        try
        {
            var notifiedUserIds = new HashSet<int> { issue.ReporterId }; // Don't notify the reporter

            // Get project members who want issue notifications
            var activeMembers = issue.Project?.ProjectUsers?
                .Where(pu => !notifiedUserIds.Contains(pu.UserId) && pu.User.IssueUpdates)
                .Select(pu => pu.User)
                .ToList() ?? new List<User>();

            // Send notification service notifications (real-time + persistent) to all project members
            if (activeMembers.Any())
            {
                await _notificationService.SendIssueCreatedNotification(
                    activeMembers,
                    issue,
                    issue.Reporter,
                    cancellationToken);
            }

            // 1. Notify assignee if someone is assigned and it's not the reporter
            if (issue.AssigneeId.HasValue && issue.Assignee != null && issue.AssigneeId != issue.ReporterId)
            {
                // Send assignment notification (real-time + email)
                await _notificationService.SendIssueAssignedNotification(
                    issue.Assignee,
                    issue,
                    issue.Reporter,
                    cancellationToken);

                notifiedUserIds.Add(issue.AssigneeId.Value);
                _logger.LogInformation("Issue assignment notification sent to {Email} for issue {IssueId}",
                    issue.Assignee.Email, issue.Id);
            }

            // 2. Send emails to project managers/owners for all new issues
            var projectManagers = issue.Project?.ProjectUsers?
                .Where(pu => pu.RoleInProject == "Admin" || pu.RoleInProject == "Owner")
                .Where(pu => !notifiedUserIds.Contains(pu.UserId) && pu.User.ProjectUpdates)
                .Select(pu => pu.User)
                .ToList() ?? new List<User>();

            foreach (var manager in projectManagers)
            {
                await _emailService.SendIssueCreatedEmailAsync(manager, issue, issue.Reporter);
                notifiedUserIds.Add(manager.Id);
                _logger.LogInformation("Issue creation email sent to project manager {Email} for issue {IssueId}",
                    manager.Email, issue.Id);
            }

            // 3. If it's a high priority issue, send additional emails to all active project members
            if (issue.Priority?.Name == "High" || issue.Priority?.Name == "Critical")
            {
                var membersForEmail = activeMembers.Where(m => !notifiedUserIds.Contains(m.Id)).ToList();
                foreach (var member in membersForEmail)
                {
                    await _emailService.SendIssueCreatedEmailAsync(member, issue, issue.Reporter);
                    notifiedUserIds.Add(member.Id);
                }

                _logger.LogInformation("High priority issue creation emails sent to {Count} project members for issue {IssueId}",
                    membersForEmail.Count, issue.Id);
            }

            // 4. Handle mentions in description
            if (!string.IsNullOrEmpty(issue.Description))
            {
                var mentionedUsernames = ExtractMentions(issue.Description);
                if (mentionedUsernames.Any())
                {
                    var mentionedUsers = await _userRepo.Query()
                        .Where(u => mentionedUsernames.Contains(u.Name) &&
                                   !notifiedUserIds.Contains(u.Id) &&
                                   u.MentionAlerts)
                        .ToListAsync(cancellationToken);

                    foreach (var mentionedUser in mentionedUsers)
                    {
                        // Create a fake comment for the mention notification
                        var fakeComment = new Comment
                        {
                            Content = issue.Description,
                            Author = issue.Reporter,
                            CreatedAt = issue.CreatedAt,
                            AuthorId = issue.ReporterId,
                            IssueId = issue.Id
                        };

                        // Send mention notification (real-time + email)
                        await _notificationService.SendMentionNotification(
                            mentionedUser,
                            issue,
                            fakeComment,
                            issue.Reporter,
                            cancellationToken);

                        _logger.LogInformation("Mention notification sent to {Email} for new issue {IssueId}",
                            mentionedUser.Email, issue.Id);
                    }
                }
            }

            _logger.LogInformation("All creation notifications processed for issue {IssueId}", issue.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending creation notifications for issue {IssueId}", issue.Id);
            // Don't throw - notification failure shouldn't break the issue creation
        }
    }

    private static List<string> ExtractMentions(string content)
    {
        var mentions = new List<string>();
        if (string.IsNullOrEmpty(content)) return mentions;

        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            if (word.StartsWith('@') && word.Length > 1)
            {
                mentions.Add(word.Substring(1).Trim(',', '.', '!', '?', ':', ';'));
            }
        }

        return mentions;
    }
}