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

namespace BugTrackr.Application.Issues.Commands;

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
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateIssueCommand> _validator;
    private readonly ILogger<UpdateIssueCommandHandler> _logger;

    public UpdateIssueCommandHandler(
        IRepository<Issue> issueRepo,
        IMapper mapper,
        IValidator<UpdateIssueCommand> validator,
        ILogger<UpdateIssueCommandHandler> logger)
    {
        _issueRepo = issueRepo;
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

            // Update specific properties (don't overwrite all)
            issue.Title = request.Title;
            issue.Description = request.Description;
            issue.AssigneeId = request.AssigneeId;
            issue.StatusId = request.StatusId;
            issue.PriorityId = request.PriorityId;
            issue.UpdatedAt = DateTime.UtcNow;

            _issueRepo.Update(issue);
            await _issueRepo.SaveChangesAsync(cancellationToken);

            // Load updated issue with navigation properties and map to DTO
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
}
