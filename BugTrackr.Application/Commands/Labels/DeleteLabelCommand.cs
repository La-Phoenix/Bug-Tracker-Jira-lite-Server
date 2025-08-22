using BugTrackr.Application.Common;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Labels.Commands;

public record DeleteLabelCommand(int Id) : IRequest<ApiResponse<string>>;

public class DeleteLabelCommandValidator : AbstractValidator<DeleteLabelCommand>
{
    public DeleteLabelCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public class DeleteLabelCommandHandler : IRequestHandler<DeleteLabelCommand, ApiResponse<string>>
{
    private readonly IRepository<Label> _labelRepo;
    private readonly IRepository<IssueLabel> _issueLabelRepo;
    private readonly IValidator<DeleteLabelCommand> _validator;
    private readonly ILogger<DeleteLabelCommandHandler> _logger;

    public DeleteLabelCommandHandler(
        IRepository<Label> labelRepo,
        IRepository<IssueLabel> issueLabelRepo,
        IValidator<DeleteLabelCommand> validator,
        ILogger<DeleteLabelCommandHandler> logger)
    {
        _labelRepo = labelRepo;
        _issueLabelRepo = issueLabelRepo;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(DeleteLabelCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage);
                return ApiResponse<string>.Failure("Validation failed", 400, errors);
            }

            // Check if label exists
            var existingLabel = await _labelRepo.GetByIdAsync(request.Id);
            if (existingLabel == null)
            {
                _logger.LogWarning("Label with ID {LabelId} not found", request.Id);
                return ApiResponse<string>.Failure($"Label with ID {request.Id} not found", 404);
            }

            // Check if label is being used by any issues
            var isLabelInUse = await _issueLabelRepo.Query()
                .AnyAsync(il => il.LabelId == request.Id, cancellationToken);

            if (isLabelInUse)
            {
                _logger.LogWarning("Cannot delete label {LabelId} as it is being used by issues", request.Id);
                return ApiResponse<string>.Failure("Cannot delete label as it is currently being used by one or more issues", 400);
            }

            // Delete the label
            _labelRepo.Delete(existingLabel);
            await _labelRepo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted label {LabelId}: {Name}", request.Id, existingLabel.Name);
            return ApiResponse<string>.SuccessResponse("Label deleted successfully", 200);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting label {LabelId}: {Message}", request.Id, ex.Message);
            return ApiResponse<string>.Failure("An unexpected error occurred.", 500);
        }
    }
}


