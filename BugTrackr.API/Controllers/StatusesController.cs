using BugTrackr.Application.Statuses.Queries;
//using BugTrackr.Application.Statuses.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BugTrackr.API.Controllers;

[ApiController]
[Route("api/statuses")]
[Authorize]
public class StatusesController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<StatusesController> _logger;

    public StatusesController(ISender mediator, ILogger<StatusesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all statuses
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllStatuses()
    {
        _logger.LogInformation("Getting all statuses");
        var result = await _mediator.Send(new GetAllStatusesQuery());
        return StatusCode(result.StatusCode, result);
    }

    ///// <summary>
    ///// Get status by ID
    ///// </summary>
    //[HttpGet("{id:int}")]
    //public async Task<IActionResult> GetStatus(int id)
    //{
    //    _logger.LogInformation("Getting status with ID: {StatusId}", id);
    //    var result = await _mediator.Send(new GetStatusByIdQuery(id));
    //    return StatusCode(result.StatusCode, result);
    //}

    ///// <summary>
    ///// Create new status (Admin only)
    ///// </summary>
    //[HttpPost]
    //[Authorize(Roles = "Admin")]
    //public async Task<IActionResult> CreateStatus([FromBody] CreateStatusCommand command)
    //{
    //    _logger.LogInformation("Creating new status: {Name}", command.Name);
    //    var result = await _mediator.Send(command);
    //    return StatusCode(result.StatusCode, result);
    //}

    ///// <summary>
    ///// Update status (Admin only)
    ///// </summary>
    //[HttpPut("{id:int}")]
    //[Authorize(Roles = "Admin")]
    //public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusCommand command)
    //{
    //    _logger.LogInformation("Updating status: {StatusId}", id);
    //    var updateCommand = command with { Id = id };
    //    var result = await _mediator.Send(updateCommand);
    //    return StatusCode(result.StatusCode, result);
    //}

    ///// <summary>
    ///// Delete status (Admin only)
    ///// </summary>
    //[HttpDelete("{id:int}")]
    //[Authorize(Roles = "Admin")]
    //public async Task<IActionResult> DeleteStatus(int id)
    //{
    //    _logger.LogInformation("Deleting status: {StatusId}", id);
    //    var result = await _mediator.Send(new DeleteStatusCommand(id));
    //    return StatusCode(result.StatusCode, result);
    //}
}
