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

public record GetIssueByIdQuery(int Id) : IRequest<ApiResponse<IssueDto>>;

public class GetIssueByIdQueryValidator : AbstractValidator<GetIssueByIdQuery>
{
    public GetIssueByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public class GetIssueByIdQueryHandler : IRequestHandler<GetIssueByIdQuery, ApiResponse<IssueDto>>
{
    private readonly IRepository<Issue> _issueRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetIssueByIdQueryHandler> _logger;

    public GetIssueByIdQueryHandler(
        IRepository<Issue> issueRepo,
        IMapper mapper,
        ILogger<GetIssueByIdQueryHandler> logger)
    {
        _issueRepo = issueRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IssueDto>> Handle(GetIssueByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var issue = await _issueRepo.Query()
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .Include(i => i.Project)
                .Include(i => i.Status)
                .Include(i => i.Priority)
                .Include(i => i.IssueLabels)
                    .ThenInclude(il => il.Label)
                .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

            if (issue == null)
            {
                _logger.LogWarning("Issue with ID {IssueId} not found", request.Id);
                return ApiResponse<IssueDto>.Failure($"Issue with ID {request.Id} not found", 404);
            }

            var issueDto = _mapper.Map<IssueDto>(issue);

            _logger.LogInformation("Retrieved issue with ID: {IssueId}", request.Id);
            return ApiResponse<IssueDto>.SuccessResponse(issueDto, 200, "Issue retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving issue: {Message}", ex.Message);
            return ApiResponse<IssueDto>.Failure("An unexpected error occurred.", 500);
        }
    }
}
