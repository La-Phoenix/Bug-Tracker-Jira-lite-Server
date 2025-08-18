using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.DTOs.Statuses;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Statuses.Queries;

public record GetAllStatusesQuery() : IRequest<ApiResponse<IEnumerable<StatusDto>>>;

public class GetAllStatusesQueryHandler : IRequestHandler<GetAllStatusesQuery, ApiResponse<IEnumerable<StatusDto>>>
{
    private readonly IRepository<Status> _statusRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllStatusesQueryHandler> _logger;

    public GetAllStatusesQueryHandler(
        IRepository<Status> statusRepo,
        IMapper mapper,
        ILogger<GetAllStatusesQueryHandler> logger)
    {
        _statusRepo = statusRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<StatusDto>>> Handle(GetAllStatusesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var statuses = await _statusRepo.Query()
                .ToListAsync(cancellationToken);

            var statusDtos = _mapper.Map<IEnumerable<StatusDto>>(statuses);

            _logger.LogInformation("Retrieved {Count} statuses", statuses.Count);
            return ApiResponse<IEnumerable<StatusDto>>.SuccessResponse(statusDtos, 200, "Statuses retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statuses: {Message}", ex.Message);
            return ApiResponse<IEnumerable<StatusDto>>.Failure("An unexpected error occurred.", 500);
        }
    }
}

