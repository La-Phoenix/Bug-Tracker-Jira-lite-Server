using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.DTOs.Priorities;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Priorities.Queries;

public record GetAllPrioritiesQuery() : IRequest<ApiResponse<IEnumerable<PriorityDto>>>;

public class GetAllPrioritiesQueryHandler : IRequestHandler<GetAllPrioritiesQuery, ApiResponse<IEnumerable<PriorityDto>>>
{
    private readonly IRepository<Priority> _priorityRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllPrioritiesQueryHandler> _logger;

    public GetAllPrioritiesQueryHandler(
        IRepository<Priority> priorityRepo,
        IMapper mapper,
        ILogger<GetAllPrioritiesQueryHandler> logger)
    {
        _priorityRepo = priorityRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<PriorityDto>>> Handle(GetAllPrioritiesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var priorities = await _priorityRepo.Query()
                .ToListAsync(cancellationToken);

            var priorityDtos = _mapper.Map<IEnumerable<PriorityDto>>(priorities);

            _logger.LogInformation("Retrieved {Count} priorities", priorities.Count);
            return ApiResponse<IEnumerable<PriorityDto>>.SuccessResponse(priorityDtos, 200, "Priorities retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving priorities: {Message}", ex.Message);
            return ApiResponse<IEnumerable<PriorityDto>>.Failure("An unexpected error occurred.", 500);
        }
    }
}

