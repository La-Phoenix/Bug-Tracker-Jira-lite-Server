using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Common.Helpers;
using BugTrackr.Application.DTOs.Issues;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Issues;

public record UpdateIssueCommand(
    int Id,
    string Title,
    string? Description,
    int? AssigneeId,
    int StatusId,
    int PriorityId,
    List<int>? LabelIds = null
) : IRequest<ApiResponse<IssueDto>>, ISkipFluentValidation;

public class UpdateIssueCommandValidator : AbstractValidator<UpdateIssueCommand>
{
    public UpdateIssueCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .MinimumLength(3);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.AssigneeId)
            .GreaterThan(0)
            .When(x => x.AssigneeId.HasValue);

        RuleFor(x => x.StatusId)
            .GreaterThan(0);

        RuleFor(x => x.PriorityId)
            .GreaterThan(0);
    }
}

public class UpdateIssueCommandHandler : IRequestHandler<UpdateIssueCommand, ApiResponse<IssueDto>>
{
    private readonly IRepository<Issue> _issueRepo;
    private readonly IRepository<Label> _labelRepo;
    private readonly IRepository<IssueLabel> _issueLabelRepo;
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateIssueCommand> _validator;
    private readonly ILogger<UpdateIssueCommandHandler> _logger;

    public UpdateIssueCommandHandler(
        IRepository<Issue> issueRepo,
        IRepository<Label> labelRepo,
        IRepository<IssueLabel> issueLabelRepo,
        IMapper mapper,
        IValidator<UpdateIssueCommand> validator,
        ILogger<UpdateIssueCommandHandler> logger)
    {
        _issueRepo = issueRepo;
        _labelRepo = labelRepo;
        _issueLabelRepo = issueLabelRepo;
        _mapper = mapper;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<IssueDto>> Handle(UpdateIssueCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(err => err.ErrorMessage);
                var resp = ApiResponse<IssueDto>.Failure("Validation failed", 400);
                resp.Errors = errors;
                return resp;
            }

            var issue = await _issueRepo.GetByIdAsync(request.Id);
            if (issue == null)
            {
                return ApiResponse<IssueDto>.Failure($"Issue with ID {request.Id} not found", 404);
            }

            // Update basic properties
            issue.Title = request.Title;
            issue.Description = request.Description;
            issue.AssigneeId = request.AssigneeId;
            issue.StatusId = request.StatusId;
            issue.PriorityId = request.PriorityId;
            issue.UpdatedAt = DateTime.UtcNow;

            _issueRepo.Update(issue);
            await _issueRepo.SaveChangesAsync(cancellationToken);

            // ✅ HANDLE LABEL UPDATES
            if (request.LabelIds != null)
            {
                await UpdateIssueLabels(issue.Id, request.LabelIds, cancellationToken);
            }

            // Load updated issue with navigation properties
            var updatedIssue = await _issueRepo.Query()
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .Include(i => i.Project)
                .Include(i => i.Status)
                .Include(i => i.Priority)
                .Include(i => i.IssueLabels)
                    .ThenInclude(il => il.Label)
                .FirstAsync(i => i.Id == issue.Id, cancellationToken);

            var issueDto = _mapper.Map<IssueDto>(updatedIssue);

            _logger.LogInformation("Issue updated successfully with ID: {IssueId}", issueDto.Id);
            return ApiResponse<IssueDto>.SuccessResponse(issueDto, 200, "Issue updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating issue: {Message}", ex.Message);
            return ApiResponse<IssueDto>.Failure("An unexpected error occurred.", 500);
        }
    }

    /// <summary>
    /// Updates the labels for an issue by removing all existing labels and adding new ones
    /// </summary>
    private async Task UpdateIssueLabels(int issueId, List<int> newLabelIds, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Remove all existing labels for this issue
            var existingIssueLabels = await _issueLabelRepo.Query()
                .Where(il => il.IssueId == issueId)
                .ToListAsync(cancellationToken);

            if (existingIssueLabels.Any())
            {
                foreach (var existingLabel in existingIssueLabels)
                {
                    _issueLabelRepo.Delete(existingLabel);
                }
                _logger.LogInformation("Removed {Count} existing labels from issue {IssueId}",
                    existingIssueLabels.Count, issueId);
            }

            // 2. Add new labels if provided
            if (newLabelIds.Any())
            {
                // Validate that all label IDs exist
                var existingLabelIds = await _labelRepo.Query()
                    .Where(l => newLabelIds.Contains(l.Id))
                    .Select(l => l.Id)
                    .ToListAsync(cancellationToken);

                var validLabelIds = newLabelIds.Where(id => existingLabelIds.Contains(id)).ToList();

                if (validLabelIds.Any())
                {
                    // Create new IssueLabel entries
                    var newIssueLabels = validLabelIds.Select(labelId => new IssueLabel
                    {
                        IssueId = issueId,
                        LabelId = labelId
                    }).ToList();

                    await _issueLabelRepo.AddRangeAsync(newIssueLabels);
                    _logger.LogInformation("Added {Count} new labels to issue {IssueId}",
                        newIssueLabels.Count, issueId);
                }

                var invalidLabelIds = newLabelIds.Except(existingLabelIds).ToList();
                if (invalidLabelIds.Any())
                {
                    _logger.LogWarning("Ignored {Count} invalid label IDs for issue {IssueId}: {LabelIds}",
                        invalidLabelIds.Count, issueId, string.Join(", ", invalidLabelIds));
                }
            }

            // Save all label changes
            await _issueLabelRepo.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating labels for issue {IssueId}: {Message}", issueId, ex.Message);
            throw;
        }
    }
}


