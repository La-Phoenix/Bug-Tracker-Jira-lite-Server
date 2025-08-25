using BugTrackr.Application.Commands.Projects;
using BugTrackr.Application.Projects.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BugTrackr.API.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(ISender mediator, ILogger<ProjectsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all projects
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllProjects()
    {
        _logger.LogInformation("Getting all projects");
        var result = await _mediator.Send(new GetAllProjectsQuery());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get project by ID
    /// </summary>
    //[HttpGet("{id:int}")]
    //public async Task<IActionResult> GetProject(int id)
    //{
    //    _logger.LogInformation("Getting project with ID: {ProjectId}", id);
    //    var result = await _mediator.Send(new GetProjectByIdQuery(id));
    //    return StatusCode(result.StatusCode, result);
    //}

    /// <summary>
    /// Get projects for current user
    /// </summary>
    [HttpGet("my-projects/{userId:int?}")]
    public async Task<IActionResult> GetMyProjects(int? userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId2))
            return BadRequest("Invalid user ID in token");

        var finalUserId = userId ?? userId2;

        _logger.LogInformation("Getting projects for user: {UserId}", finalUserId);

        var result = await _mediator.Send(new GetProjectsByUserQuery(finalUserId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get project members
    /// </summary>
    [HttpGet("{projectId:int}/members")]
    public async Task<IActionResult> GetProjectMembers(int projectId)
    {
        _logger.LogInformation("Getting members for project: {ProjectId}", projectId);
        var result = await _mediator.Send(new GetProjectMembersQuery(projectId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Add user to project
    /// </summary>
    [HttpPost("{projectId:int}/members")]
    public async Task<IActionResult> AddUserToProject(int projectId, [FromBody] AddUserToProjectCommand command)
    {
        _logger.LogInformation("Adding user to project: {ProjectId}", projectId);
        var addCommand = command with { ProjectId = projectId };
        var result = await _mediator.Send(addCommand);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Remove user from project
    /// </summary>
    [HttpDelete("{projectId:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveUserFromProject(int projectId, int userId)
    {
        _logger.LogInformation("Removing user {UserId} from project: {ProjectId}", userId, projectId);
        var result = await _mediator.Send(new RemoveUserFromProjectCommand(projectId, userId));
        return StatusCode(result.StatusCode, result);
    }


    /// <summary>
    /// Create a new project
    /// </summary>
    //[HttpPost]
    //public async Task<IActionResult> CreateProject([FromBody] CreateProjectCommand command)
    //{
    //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
    //        return BadRequest("Invalid user ID in token");

    //    _logger.LogInformation("Creating new project: {Name}", command.Name);
    //    var createCommand = command with { CreatedById = userId };
    //    var result = await _mediator.Send(createCommand);
    //    return StatusCode(result.StatusCode, result);
    //}

    /// <summary>
    /// Update an existing project
    /// </summary>
    //[HttpPut("{id:int}")]
    //public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectCommand command)
    //{
    //    _logger.LogInformation("Updating project: {ProjectId}", id);
    //    var updateCommand = command with { Id = id };
    //    var result = await _mediator.Send(updateCommand);
    //    return StatusCode(result.StatusCode, result);
    //}

    /// <summary>
    /// Delete a project
    /// </summary>
    //[HttpDelete("{id:int}")]
    //public async Task<IActionResult> DeleteProject(int id)
    //{
    //    _logger.LogInformation("Deleting project: {ProjectId}", id);
    //    var result = await _mediator.Send(new DeleteProjectCommand(id));
    //    return StatusCode(result.StatusCode, result);
    //}
}

