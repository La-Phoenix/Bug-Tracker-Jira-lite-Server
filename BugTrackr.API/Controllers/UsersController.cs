using BugTrackr.Application.Commands.Auth;
using BugTrackr.Application.Commands.Users;
using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.User;
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

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Getting profile for user: {UserId}", userId);
        var result = await _mediator.Send(new GetUserProfileQuery(userId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Updating profile for user: {UserId}", userId);

        var command = new UpdateUserProfileCommand(
            userId,
            dto.Name,
            dto.Email,
            dto.Phone,
            dto.Company,
            dto.Bio,
            dto.Timezone,
            dto.Language
        );

        var result = await _mediator.Send(command);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Upload/Update avatar
    /// </summary>
    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<AvatarUploadResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<AvatarUploadResponseDto>), 400)]
    [ProducesResponseType(typeof(ApiResponse<AvatarUploadResponseDto>), 404)]
    public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        if (dto.Avatar == null || dto.Avatar.Length == 0)
            return BadRequest("No file provided");

        _logger.LogInformation("Uploading avatar for user: {UserId}", userId);

        var command = new UploadAvatarCommand(userId, dto.Avatar);
        var result = await _mediator.Send(command);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Delete avatar
    /// </summary>
    [HttpDelete("avatar")]
    public async Task<IActionResult> DeleteAvatar()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Deleting avatar for user: {UserId}", userId);

        var command = new DeleteAvatarCommand(userId);
        var result = await _mediator.Send(command);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get notification preferences
    /// </summary>
    [HttpGet("notifications/preferences")]
    public async Task<IActionResult> GetNotificationPreferences()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Getting notification preferences for user: {UserId}", userId);
        var result = await _mediator.Send(new GetNotificationPreferencesQuery(userId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update notification preferences
    /// </summary>
    [HttpPut("notifications/preferences")]
    public async Task<IActionResult> UpdateNotificationPreferences([FromBody] NotificationPreferencesDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Updating notification preferences for user: {UserId}", userId);

        var command = new UpdateNotificationPreferencesCommand(
            userId,
            dto.EmailNotifications,
            dto.PushNotifications,
            dto.IssueUpdates,
            dto.WeeklyDigest,
            dto.MentionAlerts,
            dto.ProjectUpdates,
            dto.CommentNotifications,
            dto.AssignmentNotifications
        );

        var result = await _mediator.Send(command);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get user preferences
    /// </summary>
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Getting preferences for user: {UserId}", userId);
        var result = await _mediator.Send(new GetUserPreferencesQuery(userId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update user preferences
    /// </summary>
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UserPreferencesDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Updating preferences for user: {UserId}", userId);

        var command = new UpdateUserPreferencesCommand(
            userId,
            dto.Theme,
            dto.CompactMode,
            dto.ReducedMotion,
            dto.SidebarCollapsed,
            dto.AnimationsEnabled,
            dto.FontSize
        );

        var result = await _mediator.Send(command);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Change password
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Changing password for user: {UserId}", userId);

        var command = new ChangePasswordCommand(userId, dto.CurrentPassword, dto.NewPassword, dto.ConfirmPassword);
        var result = await _mediator.Send(command);
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

    /// <summary>
    /// Delete user (Admin only)
    /// </summary>
    //[HttpDelete("{id:int}")]
    //[Authorize(Roles = "Admin")]
    //public async Task<IActionResult> DeleteUser(int id)
    //{
    //    _logger.LogInformation("Deleting user: {UserId}", id);
    //    var result = await _mediator.Send(new DeleteUserCommand(id));
    //    return StatusCode(result.StatusCode, result);
    //}
}
