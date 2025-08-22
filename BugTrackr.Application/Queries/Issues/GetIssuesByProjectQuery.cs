using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.DTOs.Issues;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Issues.Queries;

public record GetIssuesByProjectQuery(
    int ProjectId,
    int? StatusId = null,
    int? AssigneeId = null,
    int? PriorityId = null
) : IRequest<ApiResponse<IEnumerable<IssueDto>>>;

public class GetIssuesByProjectQueryValidator : AbstractValidator<GetIssuesByProjectQuery>
{
    public GetIssuesByProjectQueryValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.StatusId).GreaterThan(0).When(x => x.StatusId.HasValue);
        RuleFor(x => x.AssigneeId).GreaterThan(0).When(x => x.AssigneeId.HasValue);
        RuleFor(x => x.PriorityId).GreaterThan(0).When(x => x.PriorityId.HasValue);
    }
}

public class GetIssuesByProjectQueryHandler : IRequestHandler<GetIssuesByProjectQuery, ApiResponse<IEnumerable<IssueDto>>>
{
    private readonly IRepository<Issue> _issueRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetIssuesByProjectQueryHandler> _logger;

    public GetIssuesByProjectQueryHandler(
        IRepository<Issue> issueRepo,
        IMapper mapper,
        ILogger<GetIssuesByProjectQueryHandler> logger)
    {
        _issueRepo = issueRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<IssueDto>>> Handle(GetIssuesByProjectQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _issueRepo.Query()
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .Include(i => i.Status)
                .Include(i => i.Priority)
                .Include(i => i.IssueLabels)
                    .ThenInclude(il => il.Label)
                .Where(i => i.ProjectId == request.ProjectId);

            // Apply filters if provided
            if (request.StatusId.HasValue)
                query = query.Where(i => i.StatusId == request.StatusId.Value);

            if (request.AssigneeId.HasValue)
                query = query.Where(i => i.AssigneeId == request.AssigneeId.Value);

            if (request.PriorityId.HasValue)
                query = query.Where(i => i.PriorityId == request.PriorityId.Value);

            var issues = await query
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);

            var issueDtos = _mapper.Map<IEnumerable<IssueDto>>(issues);

            _logger.LogInformation("Retrieved {Count} issues for project {ProjectId}", issues.Count, request.ProjectId);
            return ApiResponse<IEnumerable<IssueDto>>.SuccessResponse(issueDtos, 200, "Issues retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving issues for project {ProjectId}: {Message}", request.ProjectId, ex.Message);
            return ApiResponse<IEnumerable<IssueDto>>.Failure("An unexpected error occurred.", 500);
        }
    }
}
