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

public record CreateIssueCommand(
    string Title,
    string? Description,
    int ReporterId,
    int? AssigneeId,
    int ProjectId,
    int StatusId,
    int PriorityId,
    List<int>? LabelIds = null
) : IRequest<ApiResponse<IssueDto>>, ISkipFluentValidation;

public class CreateIssueCommandValidator : AbstractValidator<CreateIssueCommand>
{
    public CreateIssueCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .MinimumLength(3);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.ReporterId)
            .GreaterThan(0);

        RuleFor(x => x.AssigneeId)
            .GreaterThan(0)
            .When(x => x.AssigneeId.HasValue);

        RuleFor(x => x.ProjectId)
            .GreaterThan(0);

        RuleFor(x => x.StatusId)
            .GreaterThan(0);

        RuleFor(x => x.PriorityId)
            .GreaterThan(0);
    }
}

public class CreateIssueCommandHandler : IRequestHandler<CreateIssueCommand, ApiResponse<IssueDto>>
{
    private readonly IRepository<Issue> _issueRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateIssueCommandHandler> _logger;
    private readonly IValidator<CreateIssueCommand> _validator;

    public CreateIssueCommandHandler(
        IRepository<Issue> issueRepo,
        IMapper mapper,
        IValidator<CreateIssueCommand> validator,
        ILogger<CreateIssueCommandHandler> logger)
    {
        _issueRepo = issueRepo;
        _mapper = mapper;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<IssueDto>> Handle(CreateIssueCommand request, CancellationToken cancellationToken)
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

            // Use AutoMapper to map command to entity
            var issue = _mapper.Map<Issue>(request);

            await _issueRepo.AddAsync(issue);
            await _issueRepo.SaveChangesAsync(cancellationToken);

            // Load the issue with navigation properties and map to DTO
            var createdIssue = await _issueRepo.Query()
                .Include(i => i.Reporter)
                .Include(i => i.Assignee)
                .Include(i => i.Project)
                .Include(i => i.Status)
                .Include(i => i.Priority)
                .Include(i => i.IssueLabels)
                    .ThenInclude(il => il.Label)
                .FirstAsync(i => i.Id == issue.Id, cancellationToken);

            var issueDto = _mapper.Map<IssueDto>(createdIssue);

            _logger.LogInformation("Issue created successfully with ID: {IssueId}", issueDto.Id);
            return ApiResponse<IssueDto>.SuccessResponse(issueDto, 201, "Issue created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating issue: {Message}", ex.Message);
            return ApiResponse<IssueDto>.Failure("An unexpected error occurred.", 500);
        }
    }
}
