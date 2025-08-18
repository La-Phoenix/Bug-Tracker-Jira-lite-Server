using BugTrackr.Application.Labels.Queries;
//using BugTrackr.Application.Labels.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BugTrackr.API.Controllers;

[ApiController]
[Route("api/labels")]
[Authorize]
public class LabelsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<LabelsController> _logger;

    public LabelsController(ISender mediator, ILogger<LabelsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all labels
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllLabels()
    {
        _logger.LogInformation("Getting all labels");
        var result = await _mediator.Send(new GetAllLabelsQuery());
        return StatusCode(result.StatusCode, result);
    }

    ///// <summary>
    ///// Get label by ID
    ///// </summary>
    //[HttpGet("{id:int}")]
    //public async Task<IActionResult> GetLabel(int id)
    //{
    //    _logger.LogInformation("Getting label with ID: {LabelId}", id);
    //    var result = await _mediator.Send(new GetLabelByIdQuery(id));
    //    return StatusCode(result.StatusCode, result);
    //}

    ///// <summary>
    ///// Create new label
    ///// </summary>
    //[HttpPost]
    //public async Task<IActionResult> CreateLabel([FromBody] CreateLabelCommand command)
    //{
    //    _logger.LogInformation("Creating new label: {Name}", command.Name);
    //    var result = await _mediator.Send(command);
    //    return StatusCode(result.StatusCode, result);
    //}

    ///// <summary>
    ///// Update label
    ///// </summary>
    //[HttpPut("{id:int}")]
    //public async Task<IActionResult> UpdateLabel(int id, [FromBody] UpdateLabelCommand command)
    //{
    //    _logger.LogInformation("Updating label: {LabelId}", id);
    //    var updateCommand = command with { Id = id };
    //    var result = await _mediator.Send(updateCommand);
    //    return StatusCode(result.StatusCode, result);
    //}

    ///// <summary>
    ///// Delete label
    ///// </summary>
    //[HttpDelete("{id:int}")]
    //public async Task<IActionResult> DeleteLabel(int id)
    //{
    //    _logger.LogInformation("Deleting label: {LabelId}", id);
    //    var result = await _mediator.Send(new DeleteLabelCommand(id));
    //    return StatusCode(result.StatusCode, result);
    //}
}
