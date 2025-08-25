using BugTrackr.Application.Common;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetProjectsByUserQuery(int UserId) : IRequest<ApiResponse<List<Project>>>;

public class GetProjectsByUserQueryHandler : IRequestHandler<GetProjectsByUserQuery, ApiResponse<List<Project>>>
{
    private readonly IRepository<ProjectUser> _projectUserRepo;
    private readonly ILogger<GetProjectsByUserQueryHandler> _logger;

    public GetProjectsByUserQueryHandler(
        IRepository<ProjectUser> projectUserRepo,
        ILogger<GetProjectsByUserQueryHandler> logger)
    {
        _projectUserRepo = projectUserRepo;
        _logger = logger;
    }

    public async Task<ApiResponse<List<Project>>> Handle(GetProjectsByUserQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var projects = await _projectUserRepo.Query()
                .Where(pu => pu.UserId == request.UserId)
                .Select(pu => pu.Project)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (projects == null || projects.Count == 0)
            {
                _logger.LogInformation("No projects found for user {UserId}", request.UserId);
                return ApiResponse<List<Project>>.SuccessResponse(new List<Project>(), 200, "No projects found for this user.");
            }

            return ApiResponse<List<Project>>.SuccessResponse(projects, 200, "User's Projects retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving projects for user {UserId}", request.UserId);
            return ApiResponse<List<Project>>.Failure("An unexpected error occurred.", 500);
        }
    }
}
