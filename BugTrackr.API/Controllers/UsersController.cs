using BugTrackr.Application.Queries.Users;
using BugTrackr.Application.Users.Queries;
//using BugTrackr.Application.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BugTrackr.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ISender mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        _logger.LogInformation("Getting all users");
        var result = await _mediator.Send(new GetAllUsersQuery());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        _logger.LogInformation("Getting user with ID: {UserId}", id);
        var result = await _mediator.Send(new GetUserByIdQuery(id));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get users by project ID
    /// </summary>
    [HttpGet("project/{projectId:int}")]
    public async Task<IActionResult> GetUsersByProject(int projectId)
    {
        _logger.LogInformation("Getting users for project: {ProjectId}", projectId);
        var result = await _mediator.Send(new GetUsersByProjectQuery(projectId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get all team members from all projects the current user is a member of
    /// </summary>
    [HttpGet("my-project-teammates")]
    public async Task<IActionResult> GetMyProjectTeammates()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Getting all teammates from user's projects: {UserId}", userId);
        var result = await _mediator.Send(new GetTeammatesByUserProjectsQuery(userId));
        return StatusCode(result.StatusCode, result);
    }

    ///// <summary>
    ///// Update user profile
    ///// </summary>
    //[HttpPut("{id:int}")]
    //public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserCommand command)
    //{
    //    _logger.LogInformation("Updating user: {UserId}", id);
    //    var updateCommand = command with { Id = id };
    //    var result = await _mediator.Send(updateCommand);
    //    return StatusCode(result.StatusCode, result);
    //}

    ///// <summary>
    ///// Delete user (Admin only)
    ///// </summary>
    //[HttpDelete("{id:int}")]
    //[Authorize(Roles = "Admin")]
    //public async Task<IActionResult> DeleteUser(int id)
    //{
    //    _logger.LogInformation("Deleting user: {UserId}", id);
    //    var result = await _mediator.Send(new DeleteUserCommand(id));
    //    return StatusCode(result.StatusCode, result);
    //}
}
