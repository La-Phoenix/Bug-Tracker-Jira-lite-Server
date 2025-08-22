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

public record GetIssuesByUserProjectsQuery(
    int UserId,
    int? StatusId = null,
    int? PriorityId = null
) : IRequest<ApiResponse<IEnumerable<IssueDto>>>;

public class GetIssuesByUserProjectsQueryValidator : AbstractValidator<GetIssuesByUserProjectsQuery>
{
    public GetIssuesByUserProjectsQueryValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.StatusId).GreaterThan(0).When(x => x.StatusId.HasValue);
        RuleFor(x => x.PriorityId).GreaterThan(0).When(x => x.PriorityId.HasValue);
    }
}

public class GetIssuesByUserProjectsQueryHandler : IRequestHandler<GetIssuesByUserProjectsQuery, ApiResponse<IEnumerable<IssueDto>>>
{
    private readonly IRepository<Issue> _issueRepo;
    private readonly IRepository<ProjectUser> _projectUserRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetIssuesByUserProjectsQueryHandler> _logger;

    public GetIssuesByUserProjectsQueryHandler(
        IRepository<Issue> issueRepo,
        IRepository<ProjectUser> projectUserRepo,
        IMapper mapper,
        ILogger<GetIssuesByUserProjectsQueryHandler> logger)
    {
        _issueRepo = issueRepo;
        _projectUserRepo = projectUserRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<IssueDto>>> Handle(GetIssuesByUserProjectsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get all project IDs where the user is a member
            var userProjectIds = await _projectUserRepo.Query()
                .Where(pu => pu.UserId == request.UserId)
                .Select(pu => pu.ProjectId)
                .ToListAsync(cancellationToken);

            if (!userProjectIds.Any())
            {
                _logger.LogInformation("User {UserId} is not a member of any projects", request.UserId);
                return ApiResponse<IEnumerable<IssueDto>>.SuccessResponse(
                    new List<IssueDto>(), 200, "No projects found for user.");
            }

            // Get issues from all user's projects
            var query = _issueRepo.Query()
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .Include(i => i.Status)
                .Include(i => i.Priority)
                .Include(i => i.Project)
                .Include(i => i.IssueLabels)
                    .ThenInclude(il => il.Label)
                .Where(i => userProjectIds.Contains(i.ProjectId));

            // Apply optional filters
            if (request.StatusId.HasValue)
                query = query.Where(i => i.StatusId == request.StatusId.Value);

            if (request.PriorityId.HasValue)
                query = query.Where(i => i.PriorityId == request.PriorityId.Value);

            var issues = await query
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);

            var issueDtos = _mapper.Map<IEnumerable<IssueDto>>(issues);

            _logger.LogInformation("Retrieved {Count} issues from {ProjectCount} projects for user {UserId}",
                issues.Count, userProjectIds.Count, request.UserId);

            return ApiResponse<IEnumerable<IssueDto>>.SuccessResponse(issueDtos, 200, "Issues retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving issues for user {UserId}: {Message}", request.UserId, ex.Message);
            return ApiResponse<IEnumerable<IssueDto>>.Failure("An unexpected error occurred.", 500);
        }
    }
}

