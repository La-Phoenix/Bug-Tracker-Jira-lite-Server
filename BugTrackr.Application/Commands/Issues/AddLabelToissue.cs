// File: BugTrackr.Application/Commands/Issues/AddLabelToIssueCommand.cs
using BugTrackr.Application.Common;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Issues;

public record AddLabelToIssueCommand(int IssueId, int LabelId) : IRequest<ApiResponse<string>>;

public class AddLabelToIssueCommandValidator : AbstractValidator<AddLabelToIssueCommand>
{
    public AddLabelToIssueCommandValidator()
    {
        RuleFor(x => x.IssueId).GreaterThan(0);
        RuleFor(x => x.LabelId).GreaterThan(0);
    }
}

public class AddLabelToIssueCommandHandler : IRequestHandler<AddLabelToIssueCommand, ApiResponse<string>>
{
    private readonly IRepository<Issue> _issueRepo;
    private readonly IRepository<Label> _labelRepo;
    private readonly IRepository<IssueLabel> _issueLabelRepo;
    private readonly ILogger<AddLabelToIssueCommandHandler> _logger;

    public AddLabelToIssueCommandHandler(
        IRepository<Issue> issueRepo,
        IRepository<Label> labelRepo,
        IRepository<IssueLabel> issueLabelRepo,
        ILogger<AddLabelToIssueCommandHandler> logger)
    {
        _issueRepo = issueRepo;
        _labelRepo = labelRepo;
        _issueLabelRepo = issueLabelRepo;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(AddLabelToIssueCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify issue exists
            var issue = await _issueRepo.GetByIdAsync(request.IssueId);
            if (issue == null)
                return ApiResponse<string>.Failure($"Issue with ID {request.IssueId} not found", 404);

            // Verify label exists
            var label = await _labelRepo.GetByIdAsync(request.LabelId);
            if (label == null)
                return ApiResponse<string>.Failure($"Label with ID {request.LabelId} not found", 404);

            // Check if relationship already exists
            var existingIssueLabel = await _issueLabelRepo.Query()
                .FirstOrDefaultAsync(il => il.IssueId == request.IssueId && il.LabelId == request.LabelId,
                    cancellationToken);

            if (existingIssueLabel != null)
                return ApiResponse<string>.Failure("Label is already assigned to this issue", 409);

            // Create the relationship
            var issueLabel = new IssueLabel
            {
                IssueId = request.IssueId,
                LabelId = request.LabelId
            };

            await _issueLabelRepo.AddAsync(issueLabel);
            await _issueLabelRepo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added label {LabelName} to issue {IssueTitle}",
                label.Name, issue.Title);

            return ApiResponse<string>.SuccessResponse("Label added to issue successfully", 200);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding label {LabelId} to issue {IssueId}: {Message}",
                request.LabelId, request.IssueId, ex.Message);
            return ApiResponse<string>.Failure("An unexpected error occurred.", 500);
        }
    }
}
