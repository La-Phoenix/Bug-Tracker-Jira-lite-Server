using BugTrackr.Application.Priorities.Queries;
//using BugTrackr.Application.Priorities.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BugTrackr.API.Controllers;

[ApiController]
[Route("api/priorities")]
[Authorize]
public class PrioritiesController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<PrioritiesController> _logger;

    public PrioritiesController(ISender mediator, ILogger<PrioritiesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all priorities
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllPriorities()
    {
        _logger.LogInformation("Getting all priorities");
        var result = await _mediator.Send(new GetAllPrioritiesQuery());
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get priority by ID
    /// </summary>
    //[HttpGet("{id:int}")]
    //public async Task<IActionResult> GetPriority(int id)
    //{
    //    _logger.LogInformation("Getting priority with ID: {PriorityId}", id);
    //    var result = await _mediator.Send(new GetPriorityByIdQuery(id));
    //    return StatusCode(result.StatusCode, result);
    //}

    ///// <summary>
    ///// Create new priority (Admin only)
    ///// </summary>
    //[HttpPost]
    //[Authorize(Roles = "Admin")]
    //public async Task<IActionResult> CreatePriority([FromBody] CreatePriorityCommand command)
    //{
    //    _logger.LogInformation("Creating new priority: {Name}", command.Name);
    //    var result = await _mediator.Send(command);
    //    return StatusCode(result.StatusCode, result);
    //}

    ///// <summary>
    ///// Update priority (Admin only)
    ///// </summary>
    //[HttpPut("{id:int}")]
    //[Authorize(Roles = "Admin")]
    //public async Task<IActionResult> UpdatePriority(int id, [FromBody] UpdatePriorityCommand command)
    //{
    //    _logger.LogInformation("Updating priority: {PriorityId}", id);
    //    var updateCommand = command with { Id = id };
    //    var result = await _mediator.Send(updateCommand);
    //    return StatusCode(result.StatusCode, result);
    //}

    ///// <summary>
    ///// Delete priority (Admin only)
    ///// </summary>
    //[HttpDelete("{id:int}")]
    //[Authorize(Roles = "Admin")]
    //public async Task<IActionResult> DeletePriority(int id)
    //{
    //    _logger.LogInformation("Deleting priority: {PriorityId}", id);
    //    var result = await _mediator.Send(new DeletePriorityCommand(id));
    //    return StatusCode(result.StatusCode, result);
    //}
}
