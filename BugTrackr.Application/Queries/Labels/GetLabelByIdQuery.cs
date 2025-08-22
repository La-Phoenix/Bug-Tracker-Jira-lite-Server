using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.DTOs.Labels;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Labels.Queries;

public record GetLabelByIdQuery(int Id) : IRequest<ApiResponse<LabelDto>>;

public class GetLabelByIdQueryValidator : AbstractValidator<GetLabelByIdQuery>
{
    public GetLabelByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public class GetLabelByIdQueryHandler : IRequestHandler<GetLabelByIdQuery, ApiResponse<LabelDto>>
{
    private readonly IRepository<Label> _labelRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetLabelByIdQueryHandler> _logger;

    public GetLabelByIdQueryHandler(
        IRepository<Label> labelRepo,
        IMapper mapper,
        ILogger<GetLabelByIdQueryHandler> logger)
    {
        _labelRepo = labelRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<LabelDto>> Handle(GetLabelByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var label = await _labelRepo.GetByIdAsync(request.Id);

            if (label == null)
            {
                _logger.LogWarning("Label with ID {LabelId} not found", request.Id);
                return ApiResponse<LabelDto>.Failure($"Label with ID {request.Id} not found", 404);
            }

            var labelDto = _mapper.Map<LabelDto>(label);

            _logger.LogInformation("Retrieved label with ID: {LabelId}", request.Id);
            return ApiResponse<LabelDto>.SuccessResponse(labelDto, 200, "Label retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving label {LabelId}: {Message}", request.Id, ex.Message);
            return ApiResponse<LabelDto>.Failure("An unexpected error occurred.", 500);
        }
    }
}

