using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.DTOs.Issues;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Issues.Queries;

public record GetIssuesByReporterQuery(int ReporterId) : IRequest<ApiResponse<IEnumerable<IssueDto>>>;

public class GetIssuesByReporterQueryHandler : IRequestHandler<GetIssuesByReporterQuery, ApiResponse<IEnumerable<IssueDto>>>
{
    private readonly IRepository<Issue> _issueRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetIssuesByReporterQueryHandler> _logger;

    public GetIssuesByReporterQueryHandler(
        IRepository<Issue> issueRepo,
        IMapper mapper,
        ILogger<GetIssuesByReporterQueryHandler> logger)
    {
        _issueRepo = issueRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<IssueDto>>> Handle(GetIssuesByReporterQuery request, CancellationToken cancellationToken)
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
                .Where(i => i.ReporterId == request.ReporterId)
                .ToListAsync(cancellationToken);

            var issueDtos = _mapper.Map<IEnumerable<IssueDto>>(issues);

            _logger.LogInformation("Retrieved {Count} issues reported by user {UserId}", issues.Count, request.ReporterId);
            return ApiResponse<IEnumerable<IssueDto>>.SuccessResponse(issueDtos, 200, "Reported issues retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reported issues: {Message}", ex.Message);
            return ApiResponse<IEnumerable<IssueDto>>.Failure("An unexpected error occurred.", 500);
        }
    }
}
