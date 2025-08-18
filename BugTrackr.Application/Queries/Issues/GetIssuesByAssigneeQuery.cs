using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.DTOs.Issues;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Issues.Queries;

public record GetIssuesByAssigneeQuery(int AssigneeId) : IRequest<ApiResponse<IEnumerable<IssueDto>>>;

public class GetIssuesByAssigneeQueryHandler : IRequestHandler<GetIssuesByAssigneeQuery, ApiResponse<IEnumerable<IssueDto>>>
{
    private readonly IRepository<Issue> _issueRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetIssuesByAssigneeQueryHandler> _logger;

    public GetIssuesByAssigneeQueryHandler(
        IRepository<Issue> issueRepo,
        IMapper mapper,
        ILogger<GetIssuesByAssigneeQueryHandler> logger)
    {
        _issueRepo = issueRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<IssueDto>>> Handle(GetIssuesByAssigneeQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var issues = await _issueRepo.Query()
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .Include(i => i.Project)
                .Include(i => i.Status)
                .Include(i => i.Priority)
                .Include(i => i.IssueLabels).ThenInclude(il => il.Label)
                .Where(i => i.AssigneeId == request.AssigneeId)
                .ToListAsync(cancellationToken);

            var issueDtos = _mapper.Map<IEnumerable<IssueDto>>(issues);

            _logger.LogInformation("Retrieved {Count} issues assigned to user {UserId}", issues.Count, request.AssigneeId);
            return ApiResponse<IEnumerable<IssueDto>>.SuccessResponse(issueDtos, 200, "Assigned issues retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assigned issues: {Message}", ex.Message);
            return ApiResponse<IEnumerable<IssueDto>>.Failure("An unexpected error occurred.", 500);
        }
    }
}
