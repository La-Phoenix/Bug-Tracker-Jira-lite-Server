using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.DTOs.Labels;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Labels.Queries;

public record GetAllLabelsQuery() : IRequest<ApiResponse<IEnumerable<LabelDto>>>;

public class GetAllLabelsQueryHandler : IRequestHandler<GetAllLabelsQuery, ApiResponse<IEnumerable<LabelDto>>>
{
    private readonly IRepository<Label> _labelRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllLabelsQueryHandler> _logger;

    public GetAllLabelsQueryHandler(
        IRepository<Label> labelRepo,
        IMapper mapper,
        ILogger<GetAllLabelsQueryHandler> logger)
    {
        _labelRepo = labelRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<LabelDto>>> Handle(GetAllLabelsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var labels = await _labelRepo.Query()
                .ToListAsync(cancellationToken);

            var labelDtos = _mapper.Map<IEnumerable<LabelDto>>(labels);

            _logger.LogInformation("Retrieved {Count} labels", labels.Count);
            return ApiResponse<IEnumerable<LabelDto>>.SuccessResponse(labelDtos, 200, "Labels retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving labels: {Message}", ex.Message);
            return ApiResponse<IEnumerable<LabelDto>>.Failure("An unexpected error occurred.", 500);
        }
    }
}

