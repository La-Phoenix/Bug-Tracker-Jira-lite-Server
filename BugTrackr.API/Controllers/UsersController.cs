using BugTrackr.Application.Users.Queries;
//using BugTrackr.Application.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    ///// <summary>
    ///// Get user by ID
    ///// </summary>
    //[HttpGet("{id:int}")]
    //public async Task<IActionResult> GetUser(int id)
    //{
    //    _logger.LogInformation("Getting user with ID: {UserId}", id);
    //    var result = await _mediator.Send(new GetUserByIdQuery(id));
    //    return StatusCode(result.StatusCode, result);
    //}

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
