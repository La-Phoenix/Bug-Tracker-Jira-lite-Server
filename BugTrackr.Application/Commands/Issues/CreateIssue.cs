using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Common.Helpers;
using BugTrackr.Application.DTOs.Issues;
using BugTrackr.Application.Services;
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
    private readonly IMapper _mapper;
    private readonly ILogger<CreateIssueCommandHandler> _logger;
    private readonly IValidator<CreateIssueCommand> _validator;

    public CreateIssueCommandHandler(
        IRepository<Issue> issueRepo,
        IRepository<Label> labelRepo,
        IRepository<IssueLabel> issueLabelRepo,
        IMapper mapper,
        IValidator<CreateIssueCommand> validator,
        ILogger<CreateIssueCommandHandler> logger)
    {
        _issueRepo = issueRepo;
        _labelRepo = labelRepo;
        _issueLabelRepo = issueLabelRepo;
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

            var issueDto = _mapper.Map<IssueDto>(createdIssue);

            return ApiResponse<IssueDto>.SuccessResponse(issueDto, 201, "Issue created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating issue: {Message}", ex.Message);
            return ApiResponse<IssueDto>.Failure("An unexpected error occurred.", 500);
        }
    }
}