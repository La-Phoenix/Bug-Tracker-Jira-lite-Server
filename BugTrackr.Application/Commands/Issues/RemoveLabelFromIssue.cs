// File: BugTrackr.Application/Commands/Issues/RemoveLabelFromIssueCommand.cs
using BugTrackr.Application.Common;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Issues;

public record RemoveLabelFromIssueCommand(int IssueId, int LabelId) : IRequest<ApiResponse<string>>;

public class RemoveLabelFromIssueCommandValidator : AbstractValidator<RemoveLabelFromIssueCommand>
{
    public RemoveLabelFromIssueCommandValidator()
    {
        RuleFor(x => x.IssueId).GreaterThan(0);
        RuleFor(x => x.LabelId).GreaterThan(0);
    }
}

public class RemoveLabelFromIssueCommandHandler : IRequestHandler<RemoveLabelFromIssueCommand, ApiResponse<string>>
{
    private readonly IRepository<IssueLabel> _issueLabelRepo;
    private readonly ILogger<RemoveLabelFromIssueCommandHandler> _logger;

    public RemoveLabelFromIssueCommandHandler(
        IRepository<IssueLabel> issueLabelRepo,
        ILogger<RemoveLabelFromIssueCommandHandler> logger)
    {
        _issueLabelRepo = issueLabelRepo;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(RemoveLabelFromIssueCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var issueLabel = await _issueLabelRepo.Query()
                .FirstOrDefaultAsync(il => il.IssueId == request.IssueId && il.LabelId == request.LabelId,
                    cancellationToken);

            if (issueLabel == null)
                return ApiResponse<string>.Failure("Label is not assigned to this issue", 404);

            _issueLabelRepo.Delete(issueLabel);
            await _issueLabelRepo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed label {LabelId} from issue {IssueId}",
                request.LabelId, request.IssueId);

            return ApiResponse<string>.SuccessResponse("Label removed from issue successfully", 200);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing label {LabelId} from issue {IssueId}: {Message}",
                request.LabelId, request.IssueId, ex.Message);
            return ApiResponse<string>.Failure("An unexpected error occurred.", 500);
        }
    }
}

