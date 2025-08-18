using BugTrackr.Application.Common;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Issues.Commands;

public record DeleteIssueCommand(int Id) : IRequest<ApiResponse<bool>>;

public class DeleteIssueCommandHandler : IRequestHandler<DeleteIssueCommand, ApiResponse<bool>>
{
    private readonly IRepository<Issue> _issueRepo;
    private readonly ILogger<DeleteIssueCommandHandler> _logger;

    public DeleteIssueCommandHandler(
        IRepository<Issue> issueRepo,
        ILogger<DeleteIssueCommandHandler> logger)
    {
        _issueRepo = issueRepo;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteIssueCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var issue = await _issueRepo.GetByIdAsync(request.Id);
            if (issue == null)
            {
                _logger.LogWarning("Issue with ID {IssueId} not found for deletion", request.Id);
                return ApiResponse<bool>.Failure($"Issue with ID {request.Id} not found", 404);
            }

            _issueRepo.Delete(issue);
            await _issueRepo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted issue with ID: {IssueId}", request.Id);
            return ApiResponse<bool>.SuccessResponse(true, 204, "Issue deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting issue: {Message}", ex.Message);
            return ApiResponse<bool>.Failure("An unexpected error occurred.", 500);
        }
    }
}
