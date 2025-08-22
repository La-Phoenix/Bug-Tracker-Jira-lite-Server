using BugTrackr.Application.Commands.Issues;
using BugTrackr.Application.Issues.Commands;
using BugTrackr.Application.Issues.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BugTrackr.API.Controllers;

[ApiController]
[Route("api/issues")]
[Authorize]
public class IssuesController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<IssuesController> _logger;

    public IssuesController(ISender mediator, ILogger<IssuesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all issues
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllIssues()
    {
        _logger.LogInformation("Getting all issues");
        var result = await _mediator.Send(new GetAllIssuesQuery());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get issue by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetIssue(int id)
    {
        _logger.LogInformation("Getting issue with ID: {IssueId}", id);
        var result = await _mediator.Send(new GetIssueByIdQuery(id));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get issues from all projects the current user is a member of
    /// </summary>
    [HttpGet("my-projects-issues")]
    public async Task<IActionResult> GetMyProjectsIssues(
        [FromQuery] int? statusId = null,
        [FromQuery] int? priorityId = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Getting issues from all projects for user: {UserId}", userId);
        var result = await _mediator.Send(new GetIssuesByUserProjectsQuery(userId, statusId, priorityId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get issues by project ID with filtering options
    /// </summary>
    [HttpGet("project/{projectId:int}")]
    public async Task<IActionResult> GetIssuesByProject(
        int projectId,
        [FromQuery] int? statusId = null,
        [FromQuery] int? assigneeId = null,
        [FromQuery] int? priorityId = null)
    {
        _logger.LogInformation("Getting issues for project: {ProjectId}", projectId);
        var result = await _mediator.Send(new GetIssuesByProjectQuery(projectId, statusId, assigneeId, priorityId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get issues by label ID
    /// </summary>
    [HttpGet("label/{labelId:int}")]
    public async Task<IActionResult> GetIssuesByLabel(int labelId)
    {
        _logger.LogInformation("Getting issues for label: {LabelId}", labelId);
        var result = await _mediator.Send(new GetIssuesByLabelQuery(labelId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get current user's projects with issue counts
    /// </summary>
    //[HttpGet("my-projects-with-stats")]
    //public async Task<IActionResult> GetMyProjectsWithStats()
    //{
    //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
    //        return BadRequest("Invalid user ID in token");

    //    _logger.LogInformation("Getting projects with stats for user: {UserId}", userId);
    //    var result = await _mediator.Send(new GetProjectsWithStatsByUserQuery(userId));
    //    return StatusCode(result.StatusCode, result);
    //}

    /// <summary>
    /// Create a new issue
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateIssue([FromBody] CreateIssueCommand command)
    {
        _logger.LogInformation("Creating new issue: {Title}", command.Title);
        var result = await _mediator.Send(command);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Add label to existing issue
    /// </summary>
    [HttpPost("{issueId:int}/labels/{labelId:int}")]
    public async Task<IActionResult> AddLabelToIssue(int issueId, int labelId)
    {
        _logger.LogInformation("Adding label {LabelId} to issue {IssueId}", labelId, issueId);
        var result = await _mediator.Send(new AddLabelToIssueCommand(issueId, labelId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Remove label from issue
    /// </summary>
    [HttpDelete("{issueId:int}/labels/{labelId:int}")]
    public async Task<IActionResult> RemoveLabelFromIssue(int issueId, int labelId)
    {
        _logger.LogInformation("Removing label {LabelId} from issue {IssueId}", labelId, issueId);
        var result = await _mediator.Send(new RemoveLabelFromIssueCommand(issueId, labelId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update an existing issue
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateIssue(int id, [FromBody] UpdateIssueCommand command)
    {
        _logger.LogInformation("Updating issue: {IssueId}", id);
        var updateCommand = command with { Id = id };
        var result = await _mediator.Send(updateCommand);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Delete an issue
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteIssue(int id)
    {
        _logger.LogInformation("Deleting issue: {IssueId}", id);
        var result = await _mediator.Send(new DeleteIssueCommand(id));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get current user's assigned issues
    /// </summary>
    [HttpGet("my-assignments")]
    public async Task<IActionResult> GetMyAssignedIssues()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Getting assigned issues for user: {UserId}", userId);
        var result = await _mediator.Send(new GetIssuesByAssigneeQuery(userId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get current user's reported issues
    /// </summary>
    [HttpGet("my-reports")]
    public async Task<IActionResult> GetMyReportedIssues()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Getting reported issues for user: {UserId}", userId);
        var result = await _mediator.Send(new GetIssuesByReporterQuery(userId));
        return StatusCode(result.StatusCode, result);
    }
}
