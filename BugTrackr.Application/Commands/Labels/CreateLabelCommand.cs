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

public record CreateLabelCommand(string Name, string Color) : IRequest<ApiResponse<LabelDto>>;

public class CreateLabelCommandValidator : AbstractValidator<CreateLabelCommand>
{
    public CreateLabelCommandValidator()
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

public class CreateLabelCommandHandler : IRequestHandler<CreateLabelCommand, ApiResponse<LabelDto>>
{
    private readonly IRepository<Label> _labelRepo;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateLabelCommand> _validator;
    private readonly ILogger<CreateLabelCommandHandler> _logger;

    public CreateLabelCommandHandler(
        IRepository<Label> labelRepo,
        IMapper mapper,
        IValidator<CreateLabelCommand> validator,
        ILogger<CreateLabelCommandHandler> logger)
    {
        _labelRepo = labelRepo;
        _mapper = mapper;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<LabelDto>> Handle(CreateLabelCommand request, CancellationToken cancellationToken)
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

            // Check if label name already exists
            var existingLabel = await _labelRepo.Query()
                .FirstOrDefaultAsync(l => l.Name.ToLower() == request.Name.ToLower(), cancellationToken);

            if (existingLabel != null)
            {
                return ApiResponse<LabelDto>.Failure("Label with this name already exists", 409);
            }

            // Create new label
            var label = new Label
            {
                Name = request.Name,
                Color = request.Color
            };

            await _labelRepo.AddAsync(label);
            await _labelRepo.SaveChangesAsync(cancellationToken);

            var labelDto = _mapper.Map<LabelDto>(label);

            _logger.LogInformation("Created new label: {Name} with color {Color}", request.Name, request.Color);
            return ApiResponse<LabelDto>.SuccessResponse(labelDto, 201, "Label created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating label {Name}: {Message}", request.Name, ex.Message);
            return ApiResponse<LabelDto>.Failure("An unexpected error occurred.", 500);
        }
    }
}

