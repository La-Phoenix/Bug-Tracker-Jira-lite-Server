using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.DTOs.Labels;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Labels.Commands;

public record UpdateLabelCommand(int Id, string Name, string Color) : IRequest<ApiResponse<LabelDto>>;

public class UpdateLabelCommandValidator : AbstractValidator<UpdateLabelCommand>
{
    public UpdateLabelCommandValidator()
    {

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Color)
            .NotEmpty()
            .Matches(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
            .WithMessage("Color must be a valid hex color code (e.g., #ff0000)");
    }
}

public class UpdateLabelCommandHandler : IRequestHandler<UpdateLabelCommand, ApiResponse<LabelDto>>
{
    private readonly IRepository<Label> _labelRepo;
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateLabelCommand> _validator;
    private readonly ILogger<UpdateLabelCommandHandler> _logger;

    public UpdateLabelCommandHandler(
        IRepository<Label> labelRepo,
        IMapper mapper,
        IValidator<UpdateLabelCommand> validator,
        ILogger<UpdateLabelCommandHandler> logger)
    {
        _labelRepo = labelRepo;
        _mapper = mapper;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<LabelDto>> Handle(UpdateLabelCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage);
                return ApiResponse<LabelDto>.Failure("Validation failed", 400, errors);
            }

            // Check if label exists
            var existingLabel = await _labelRepo.GetByIdAsync(request.Id);
            if (existingLabel == null)
            {
                _logger.LogWarning("Label with ID {LabelId} not found", request.Id);
                return ApiResponse<LabelDto>.Failure($"Label with ID {request.Id} not found", 404);
            }

            // Check if another label with the same name exists (excluding current label)
            var duplicateNameLabel = await _labelRepo.Query()
                .FirstOrDefaultAsync(l => l.Name.ToLower() == request.Name.ToLower() && l.Id != request.Id, cancellationToken);

            if (duplicateNameLabel != null)
            {
                return ApiResponse<LabelDto>.Failure("Another label with this name already exists", 409);
            }

            // Update the label properties
            existingLabel.Name = request.Name;
            existingLabel.Color = request.Color;

            _labelRepo.Update(existingLabel);
            await _labelRepo.SaveChangesAsync(cancellationToken);

            var labelDto = _mapper.Map<LabelDto>(existingLabel);

            _logger.LogInformation("Updated label {LabelId}: Name={Name}, Color={Color}",
                request.Id, request.Name, request.Color);

            return ApiResponse<LabelDto>.SuccessResponse(labelDto, 200, "Label updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating label {LabelId}: {Message}", request.Id, ex.Message);
            return ApiResponse<LabelDto>.Failure("An unexpected error occurred.", 500);
        }
    }
}


