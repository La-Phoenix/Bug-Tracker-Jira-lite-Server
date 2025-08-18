using BugTrackr.Application.Projects.Queries;
//using BugTrackr.Application.Projects.Commands;
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

    ///// <summary>
    ///// Get project by ID
    ///// </summary>
    //[HttpGet("{id:int}")]
    //public async Task<IActionResult> GetProject(int id)
    //{
    //    _logger.LogInformation("Getting project with ID: {ProjectId}", id);
    //    var result = await _mediator.Send(new GetProjectByIdQuery(id));
    //    return StatusCode(result.StatusCode, result);
    //}

    ///// <summary>
    ///// Create a new project
    ///// </summary>
    //[HttpPost]
    //public async Task<IActionResult> CreateProject([FromBody] CreateProjectCommand command)
    //{
    //    _logger.LogInformation("Creating new project: {Name}", command.Name);
    //    var result = await _mediator.Send(command);
    //    return StatusCode(result.StatusCode, result);
    //}

    ///// <summary>
    ///// Update an existing project
    ///// </summary>
    //[HttpPut("{id:int}")]
    //public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectCommand command)
    //{
    //    _logger.LogInformation("Updating project: {ProjectId}", id);
    //    var updateCommand = command with { Id = id };
    //    var result = await _mediator.Send(updateCommand);
    //    return StatusCode(result.StatusCode, result);
    //}

    ///// <summary>
    ///// Delete a project
    ///// </summary>
    //[HttpDelete("{id:int}")]
    //public async Task<IActionResult> DeleteProject(int id)
    //{
    //    _logger.LogInformation("Deleting project: {ProjectId}", id);
    //    var result = await _mediator.Send(new DeleteProjectCommand(id));
    //    return StatusCode(result.StatusCode, result);
    //}

    ///// <summary>
    ///// Get projects for current user
    ///// </summary>
    //[HttpGet("my-projects")]
    //public async Task<IActionResult> GetMyProjects()
    //{
    //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
    //        return BadRequest("Invalid user ID in token");

    //    _logger.LogInformation("Getting projects for user: {UserId}", userId);
    //    var result = await _mediator.Send(new GetProjectsByUserQuery(userId));
    //    return StatusCode(result.StatusCode, result);
    //}
}
