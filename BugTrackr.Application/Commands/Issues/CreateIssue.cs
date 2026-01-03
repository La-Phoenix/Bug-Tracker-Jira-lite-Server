using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Common.Helpers;
using BugTrackr.Application.DTOs.Issues;
using BugTrackr.Application.Services;
using BugTrackr.Application.Services.Email;
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
    private readonly IMapper _mapper;
    private readonly ILogger<CreateIssueCommandHandler> _logger;
    private readonly IValidator<CreateIssueCommand> _validator;

    public CreateIssueCommandHandler(
        IRepository<Issue> issueRepo,
        IRepository<Label> labelRepo,
        IRepository<IssueLabel> issueLabelRepo,
        IRepository<User> userRepo,
        IEmailService emailService,
        IMapper mapper,
        IValidator<CreateIssueCommand> validator,
        ILogger<CreateIssueCommandHandler> logger)
    {
        _issueRepo = issueRepo;
        _labelRepo = labelRepo;
        _issueLabelRepo = issueLabelRepo;
        _userRepo = userRepo;
        _emailService = emailService;
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
            await SendCreationNotificationsAsync(createdIssue);

            var issueDto = _mapper.Map<IssueDto>(createdIssue);

            return ApiResponse<IssueDto>.SuccessResponse(issueDto, 201, "Issue created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating issue: {Message}", ex.Message);
            return ApiResponse<IssueDto>.Failure("An unexpected error occurred.", 500);
        }
    }
    private async Task SendCreationNotificationsAsync(Issue issue)
    {
        try
        {
            var notifiedUserIds = new HashSet<int> { issue.ReporterId }; // Don't notify the reporter

            // 1. Notify assignee if someone is assigned and it's not the reporter
            if (issue.AssigneeId.HasValue && issue.Assignee != null && issue.AssigneeId != issue.ReporterId)
            {
                await _emailService.SendIssueAssignedEmailAsync(issue.Assignee, issue, issue.Reporter);
                notifiedUserIds.Add(issue.AssigneeId.Value);
                _logger.LogInformation("Issue assignment notification sent to {Email} for issue {IssueId}",
                    issue.Assignee.Email, issue.Id);
            }

            // 2. Notify project managers/owners using the dedicated issue creation email
            var projectManagers = issue.Project?.ProjectUsers?
                .Where(pu => pu.RoleInProject == "Admin" || pu.RoleInProject == "Owner")
                .Where(pu => !notifiedUserIds.Contains(pu.UserId) && pu.User.ProjectUpdates)
                .Select(pu => pu.User)
                .ToList() ?? new List<User>();

            foreach (var manager in projectManagers)
            {
                await _emailService.SendIssueCreatedEmailAsync(manager, issue, issue.Reporter);
                notifiedUserIds.Add(manager.Id);
                _logger.LogInformation("Issue creation notification sent to project manager {Email} for issue {IssueId}",
                    manager.Email, issue.Id);
            }

            // 3. If it's a high priority issue, notify all active project members with issue creation email
            if (issue.Priority?.Name == "High" || issue.Priority?.Name == "Critical")
            {
                var activeMembers = issue.Project?.ProjectUsers?
                    .Where(pu => !notifiedUserIds.Contains(pu.UserId) && pu.User.IssueUpdates)
                    .Select(pu => pu.User)
                    .ToList() ?? new List<User>();

                foreach (var member in activeMembers)
                {
                    await _emailService.SendIssueCreatedEmailAsync(member, issue, issue.Reporter);
                    notifiedUserIds.Add(member.Id);
                }

                _logger.LogInformation("High priority issue creation notifications sent to {Count} project members for issue {IssueId}",
                    activeMembers.Count, issue.Id);
            }

            // 4. If issue mentions users in description, notify them
            if (!string.IsNullOrEmpty(issue.Description))
            {
                var mentionedUsernames = ExtractMentions(issue.Description);
                if (mentionedUsernames.Any())
                {
                    var mentionedUsers = await _userRepo.Query()
                        .Where(u => mentionedUsernames.Contains(u.Name) &&
                                   !notifiedUserIds.Contains(u.Id) &&
                                   u.MentionAlerts)
                        .ToListAsync();

                    foreach (var mentionedUser in mentionedUsers)
                    {
                        await _emailService.SendMentionNotificationEmailAsync(
                            mentionedUser,
                            issue,
                            new Comment { Content = issue.Description, Author = issue.Reporter, CreatedAt = issue.CreatedAt },
                            issue.Reporter
                        );
                        _logger.LogInformation("Mention notification sent to {Email} for new issue {IssueId}",
                            mentionedUser.Email, issue.Id);
                    }
                }
            }

            // 5. Notify other stakeholders who should know about new issues in the project
            //var otherStakeholders = issue.Project?.ProjectUsers?
            //    .Where(pu => pu.RoleInProject == "Lead" || pu.RoleInProject == "Manager")
            //    .Where(pu => !notifiedUserIds.Contains(pu.UserId) && pu.User.IssueUpdates)
            //    .Select(pu => pu.User)
            //    .ToList() ?? new List<User>();

            //foreach (var stakeholder in otherStakeholders)
            //{
            //    await _emailService.SendIssueCreatedEmailAsync(stakeholder, issue, issue.Reporter);
            //    notifiedUserIds.Add(stakeholder.Id);
            //}

            //if (otherStakeholders.Any())
            //{
            //    _logger.LogInformation("Issue creation notifications sent to {Count} stakeholders for issue {IssueId}",
            //        otherStakeholders.Count, issue.Id);
            //}
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