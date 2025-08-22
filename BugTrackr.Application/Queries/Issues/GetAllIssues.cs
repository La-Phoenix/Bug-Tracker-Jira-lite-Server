using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.DTOs.Issues;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Issues.Queries;

public record GetAllIssuesQuery() : IRequest<ApiResponse<IEnumerable<IssueDto>>>;

public class GetAllIssuesQueryHandler : IRequestHandler<GetAllIssuesQuery, ApiResponse<IEnumerable<IssueDto>>>
{
    private readonly IRepository<Issue> _issueRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllIssuesQueryHandler> _logger;

    public GetAllIssuesQueryHandler(
        IRepository<Issue> issueRepo,
        IMapper mapper,
        ILogger<GetAllIssuesQueryHandler> logger)
    {
        _issueRepo = issueRepo;
        _mapper = mapper;
        _logger = logger;
    }

    // In GetAllIssuesQueryHandler
    public async Task<ApiResponse<IEnumerable<IssueDto>>> Handle(GetAllIssuesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var issues = await _issueRepo.Query()
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .Include(i => i.Project)
                .Include(i => i.Status)
                .Include(i => i.Priority)
                .Include(i => i.IssueLabels)
                    .ThenInclude(il => il.Label)
                .ToListAsync(cancellationToken);

            // ✅ ADD DEBUGGING
            foreach (var issue in issues)
            {
                _logger.LogInformation("Issue {IssueId}: {Title} has {LabelCount} labels",
                    issue.Id, issue.Title, issue.IssueLabels?.Count ?? 0);

                if (issue.IssueLabels != null && issue.IssueLabels.Any())
                {
                    foreach (var issueLabel in issue.IssueLabels)
                    {
                        _logger.LogInformation("  - Label: {LabelName} (ID: {LabelId})",
                            issueLabel.Label?.Name, issueLabel.LabelId);
                    }
                }
            }

            var issueDtos = _mapper.Map<IEnumerable<IssueDto>>(issues);

            // ✅ ADD MORE DEBUGGING  
            foreach (var dto in issueDtos)
            {
                _logger.LogInformation("Mapped Issue {IssueId}: {Title} has {LabelCount} label names: [{Labels}]",
                    dto.Id, dto.Title, dto.Labels.Count, string.Join(", ", dto.Labels));
            }

            _logger.LogInformation("Retrieved {Count} issues", issues.Count);
            return ApiResponse<IEnumerable<IssueDto>>.SuccessResponse(issueDtos, 200, "Issues retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving issues: {Message}", ex.Message);
            return ApiResponse<IEnumerable<IssueDto>>.Failure("An unexpected error occurred.", 500);
        }
    }
}
