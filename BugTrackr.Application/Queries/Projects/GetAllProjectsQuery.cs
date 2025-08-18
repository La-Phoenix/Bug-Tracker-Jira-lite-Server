using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.DTOs.Projects;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Projects.Queries;

public record GetAllProjectsQuery() : IRequest<ApiResponse<IEnumerable<ProjectDto>>>;

public class GetAllProjectsQueryHandler : IRequestHandler<GetAllProjectsQuery, ApiResponse<IEnumerable<ProjectDto>>>
{
    private readonly IRepository<Project> _projectRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllProjectsQueryHandler> _logger;

    public GetAllProjectsQueryHandler(
        IRepository<Project> projectRepo,
        IMapper mapper,
        ILogger<GetAllProjectsQueryHandler> logger)
    {
        _projectRepo = projectRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<ProjectDto>>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var projects = await _projectRepo.Query()
                .Include(p => p.CreatedBy)
                .Include(p => p.ProjectUsers)
                    .ThenInclude(pu => pu.User)
                .Include(p => p.Issues)
                .ToListAsync(cancellationToken);

            var projectDtos = _mapper.Map<IEnumerable<ProjectDto>>(projects);

            _logger.LogInformation("Retrieved {Count} projects", projects.Count);
            return ApiResponse<IEnumerable<ProjectDto>>.SuccessResponse(projectDtos, 200, "Projects retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving projects: {Message}", ex.Message);
            return ApiResponse<IEnumerable<ProjectDto>>.Failure("An unexpected error occurred.", 500);
        }
    }
}

