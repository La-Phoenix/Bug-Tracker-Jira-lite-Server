using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.DTOs.Projects;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Projects.Queries;

public record GetProjectMembersQuery(int ProjectId) : IRequest<ApiResponse<IEnumerable<ProjectMemberDto>>>;

public class GetProjectMembersQueryValidator : AbstractValidator<GetProjectMembersQuery>
{
    public GetProjectMembersQueryValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
    }
}

public class GetProjectMembersQueryHandler : IRequestHandler<GetProjectMembersQuery, ApiResponse<IEnumerable<ProjectMemberDto>>>
{
    private readonly IRepository<ProjectUser> _projectUserRepo;
    private readonly IRepository<Project> _projectRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetProjectMembersQueryHandler> _logger;

    public GetProjectMembersQueryHandler(
        IRepository<ProjectUser> projectUserRepo,
        IRepository<Project> projectRepo,
        IMapper mapper,
        ILogger<GetProjectMembersQueryHandler> logger)
    {
        _projectUserRepo = projectUserRepo;
        _projectRepo = projectRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<ProjectMemberDto>>> Handle(GetProjectMembersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if project exists
            var project = await _projectRepo.GetByIdAsync(request.ProjectId);
            if (project == null)
            {
                return ApiResponse<IEnumerable<ProjectMemberDto>>.Failure($"Project with ID {request.ProjectId} not found", 404);
            }

            // Get all project members with their user details
            var projectMembers = await _projectUserRepo.Query()
                .Include(pu => pu.User)
                .Include(pu => pu.Project)
                .Where(pu => pu.ProjectId == request.ProjectId)
                .ToListAsync(cancellationToken);

            var memberDtos = projectMembers.Select(pu => new ProjectMemberDto
            {
                UserId = pu.UserId,
                UserName = pu.User.Name,
                UserEmail = pu.User.Email,
                RoleInProject = pu.RoleInProject,
                ProjectId = pu.ProjectId,
                ProjectName = pu.Project.Name
            }).ToList();

            _logger.LogInformation("Retrieved {Count} members for project {ProjectId}", memberDtos.Count, request.ProjectId);
            return ApiResponse<IEnumerable<ProjectMemberDto>>.SuccessResponse(memberDtos, 200, "Project members retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving members for project {ProjectId}: {Message}", request.ProjectId, ex.Message);
            return ApiResponse<IEnumerable<ProjectMemberDto>>.Failure("An unexpected error occurred.", 500);
        }
    }
}

