using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.DTOs.Users;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Queries.Users;

public record GetTeammatesByUserProjectsQuery(int UserId) : IRequest<ApiResponse<IEnumerable<TeamMemberDto>>>;

public class GetTeammatesByUserProjectsQueryValidator : AbstractValidator<GetTeammatesByUserProjectsQuery>
{
    public GetTeammatesByUserProjectsQueryValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}

public class GetTeammatesByUserProjectsQueryHandler : IRequestHandler<GetTeammatesByUserProjectsQuery, ApiResponse<IEnumerable<TeamMemberDto>>>
{
    private readonly IRepository<ProjectUser> _projectUserRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetTeammatesByUserProjectsQueryHandler> _logger;

    public GetTeammatesByUserProjectsQueryHandler(
        IRepository<ProjectUser> projectUserRepo,
        IMapper mapper,
        ILogger<GetTeammatesByUserProjectsQueryHandler> logger)
    {
        _projectUserRepo = projectUserRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<TeamMemberDto>>> Handle(GetTeammatesByUserProjectsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get all project IDs where the user is a member
            var userProjectIds = await _projectUserRepo.Query()
                .Where(pu => pu.UserId == request.UserId)
                .Select(pu => pu.ProjectId)
                .ToListAsync(cancellationToken);

            if (!userProjectIds.Any())
            {
                _logger.LogInformation("User {UserId} is not a member of any projects", request.UserId);
                return ApiResponse<IEnumerable<TeamMemberDto>>.SuccessResponse(
                    new List<TeamMemberDto>(), 200, "No projects found for user.");
            }

            // Get all unique teammates from all user's projects
            var teammates = await _projectUserRepo.Query()
                .Include(pu => pu.User)
                .Include(pu => pu.Project)
                .Where(pu => userProjectIds.Contains(pu.ProjectId) && pu.UserId != request.UserId) // Exclude the user themselves
                .GroupBy(pu => pu.UserId) // Group by user to avoid duplicates
                .Select(g => g.First()) // Take first occurrence of each user
                .ToListAsync(cancellationToken);

            var teammateDtos = teammates.Select(pu => new TeamMemberDto
            {
                UserId = pu.UserId,
                UserName = pu.User.Name,
                UserEmail = pu.User.Email,
                UserRole = pu.User.Role,
                // Get all projects this teammate shares with the requesting user
                SharedProjects = _projectUserRepo.Query()
                    .Include(p => p.Project)
                    .Where(p => p.UserId == pu.UserId && userProjectIds.Contains(p.ProjectId))
                    .Select(p => new SharedProjectInfo
                    {
                        ProjectId = p.ProjectId,
                        ProjectName = p.Project.Name,
                        RoleInProject = p.RoleInProject
                    })
                    .ToList()
            }).ToList();

            _logger.LogInformation("Retrieved {Count} unique teammates from {ProjectCount} projects for user {UserId}",
                teammateDtos.Count, userProjectIds.Count, request.UserId);

            return ApiResponse<IEnumerable<TeamMemberDto>>.SuccessResponse(teammateDtos, 200, "Teammates retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving teammates for user {UserId}: {Message}", request.UserId, ex.Message);
            return ApiResponse<IEnumerable<TeamMemberDto>>.Failure("An unexpected error occurred.", 500);
        }
    }
}
