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

public record GetIssuesByLabelQuery(int LabelId) : IRequest<ApiResponse<IEnumerable<IssueDto>>>;

public class GetIssuesByLabelQueryValidator : AbstractValidator<GetIssuesByLabelQuery>
{
    public GetIssuesByLabelQueryValidator()
    {
        RuleFor(x => x.LabelId).GreaterThan(0);
    }
}

public class GetIssuesByLabelQueryHandler : IRequestHandler<GetIssuesByLabelQuery, ApiResponse<IEnumerable<IssueDto>>>
{
    private readonly IRepository<Issue> _issueRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetIssuesByLabelQueryHandler> _logger;

    public GetIssuesByLabelQueryHandler(
        IRepository<Issue> issueRepo,
        IMapper mapper,
        ILogger<GetIssuesByLabelQueryHandler> logger)
    {
        _issueRepo = issueRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<IssueDto>>> Handle(GetIssuesByLabelQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var issues = await _issueRepo.Query()
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .Include(i => i.Status)
                .Include(i => i.Priority)
                .Include(i => i.Project)
                .Include(i => i.IssueLabels)
                    .ThenInclude(il => il.Label)
                .Where(i => i.IssueLabels.Any(il => il.LabelId == request.LabelId))
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);

            var issueDtos = _mapper.Map<IEnumerable<IssueDto>>(issues);

            _logger.LogInformation("Retrieved {Count} issues for label {LabelId}", issues.Count, request.LabelId);
            return ApiResponse<IEnumerable<IssueDto>>.SuccessResponse(issueDtos, 200, "Issues retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving issues for label {LabelId}: {Message}", request.LabelId, ex.Message);
            return ApiResponse<IEnumerable<IssueDto>>.Failure("An unexpected error occurred.", 500);
        }
    }
}

