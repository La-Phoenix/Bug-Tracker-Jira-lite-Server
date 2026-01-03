using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Services;
using BugTrackr.Application.Services.Email;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Projects;

public record AddUserToProjectCommand(int ProjectId, int UserId, string RoleInProject = "Member")
    : IRequest<ApiResponse<string>>;

public class AddUserToProjectCommandValidator : AbstractValidator<AddUserToProjectCommand>
{
    public AddUserToProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.RoleInProject).NotEmpty().MaximumLength(50);
    }
}

public class AddUserToProjectCommandHandler : IRequestHandler<AddUserToProjectCommand, ApiResponse<string>>
{
    private readonly IRepository<ProjectUser> _projectUserRepo;
    private readonly IRepository<Project> _projectRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IEmailService _emailService;
    private readonly ILogger<AddUserToProjectCommandHandler> _logger;

    public AddUserToProjectCommandHandler(
        IRepository<ProjectUser> projectUserRepo,
        IRepository<Project> projectRepo,
        IRepository<User> userRepo,
        IEmailService emailService,
        ILogger<AddUserToProjectCommandHandler> logger)
    {
        _projectUserRepo = projectUserRepo;
        _projectRepo = projectRepo;
        _userRepo = userRepo;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(AddUserToProjectCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if project exists
            var project = await _projectRepo.GetByIdAsync(request.ProjectId);
            if (project == null)
            {
                return ApiResponse<string>.Failure($"Project with ID {request.ProjectId} not found", 404);
            }

            // Check if user exists
            var user = await _userRepo.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return ApiResponse<string>.Failure($"User with ID {request.UserId} not found", 404);
            }

            // Check if user is already in project
            var existingProjectUser = await _projectUserRepo.Query()
                .FirstOrDefaultAsync(pu => pu.ProjectId == request.ProjectId && pu.UserId == request.UserId, cancellationToken);

            if (existingProjectUser != null)
            {
                return ApiResponse<string>.Failure("User is already a member of this project", 409);
            }

            // Add user to project
            var projectUser = new ProjectUser
            {
                ProjectId = request.ProjectId,
                UserId = request.UserId,
                RoleInProject = request.RoleInProject
            };

            await _projectUserRepo.AddAsync(projectUser);
            await _projectUserRepo.SaveChangesAsync(cancellationToken);

            // Send project invitation email
            await _emailService.SendProjectInvitationEmailAsync(user, project, project.CreatedBy);

            _logger.LogInformation("Added user {UserId} to project {ProjectId} with role {Role}",
                request.UserId, request.ProjectId, request.RoleInProject);

            return ApiResponse<string>.SuccessResponse("User added to project successfully", 201);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to project {ProjectId}: {Message}",
                request.UserId, request.ProjectId, ex.Message);
            return ApiResponse<string>.Failure("An unexpected error occurred.", 500);
        }
    }
}
