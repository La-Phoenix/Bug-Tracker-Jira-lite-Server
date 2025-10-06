using BugTrackr.Application.Commands.Chat;
using BugTrackr.Application.Dtos.Chat;
using BugTrackr.Application.Queries.Chat;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BugTrackr.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ISender mediator, ILogger<ChatController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all chat rooms for current user
    /// </summary>
    [HttpGet("rooms")]
    public async Task<IActionResult> GetChatRooms()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Getting chat rooms for user: {UserId}", userId);

        var result = await _mediator.Send(new GetChatRoomsQuery(userId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Create new chat room
    /// </summary>
    [HttpPost("rooms")]
    public async Task<IActionResult> CreateChatRoom([FromBody] CreateChatRoomDto createDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Creating chat room: {Name}", createDto.Name);

        var result = await _mediator.Send(new CreateChatRoomCommand(createDto, userId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get messages for a chat room
    /// </summary>
    [HttpGet("rooms/{roomId:int}/messages")]
    public async Task<IActionResult> GetMessages(
        int roomId,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50,
        [FromQuery] DateTime? before = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Getting messages for room: {RoomId}, User: {UserId}", roomId, userId);

        var result = await _mediator.Send(new GetChatMessagesQuery(roomId, userId, page, limit, before));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Send message to chat room
    /// </summary>
    [HttpPost("rooms/{roomId:int}/messages")]
    public async Task<IActionResult> SendMessage(int roomId, [FromBody] SendMessageDto messageDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Sending message to room: {RoomId}, User: {UserId}", roomId, userId);

        var result = await _mediator.Send(new SendMessageCommand(roomId, messageDto, userId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Add participants to chat room
    /// </summary>
    [HttpPost("rooms/{roomId:int}/participants")]
    public async Task<IActionResult> AddParticipants(int roomId, [FromBody] AddParticipantsDto participantsDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Adding participants to room: {RoomId}", roomId);

        var result = await _mediator.Send(new AddParticipantsCommand(roomId, participantsDto.UserIds, userId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Remove participant from chat room
    /// </summary>
    [HttpDelete("rooms/{roomId:int}/participants/{participantUserId:int}")]
    public async Task<IActionResult> RemoveParticipant(int roomId, int participantUserId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Removing participant {ParticipantId} from room: {RoomId}", participantUserId, roomId);

        var result = await _mediator.Send(new RemoveParticipantCommand(roomId, participantUserId, userId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Toggle pin status for chat room
    /// </summary>
    [HttpPut("rooms/{roomId:int}/pin")]
    public async Task<IActionResult> TogglePin(int roomId, [FromBody] TogglePinDto toggleDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Toggling pin for room: {RoomId}, User: {UserId}", roomId, userId);

        var result = await _mediator.Send(new TogglePinCommand(roomId, userId, toggleDto.IsPinned));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Toggle mute status for chat room
    /// </summary>
    [HttpPut("rooms/{roomId:int}/mute")]
    public async Task<IActionResult> ToggleMute(int roomId, [FromBody] ToggleMuteDto toggleDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Toggling mute for room: {RoomId}, User: {UserId}", roomId, userId);

        var result = await _mediator.Send(new ToggleMuteCommand(roomId, userId, toggleDto.IsMuted));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Mark messages as read
    /// </summary>
    [HttpPost("messages/{messageId:int}/read")]
    public async Task<IActionResult> MarkAsRead(int messageId, [FromBody] MarkAsReadDto readDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Marking message as read: {MessageId}, User: {UserId}", messageId, userId);

        var result = await _mediator.Send(new MarkMessageAsReadCommand(messageId, readDto.RoomId, userId, readDto.LastMessageId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Edit message
    /// </summary>
    [HttpPut("messages/{messageId:int}")]
    public async Task<IActionResult> EditMessage(int messageId, [FromBody] EditMessageDto editDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Editing message: {MessageId}, User: {UserId}", messageId, userId);

        var result = await _mediator.Send(new EditMessageCommand(messageId, editDto.Content, userId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Delete message
    /// </summary>
    [HttpDelete("messages/{messageId:int}")]
    public async Task<IActionResult> DeleteMessage(int messageId, [FromBody] DeleteMessageDto deleteDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Deleting message: {MessageId}, User: {UserId}", messageId, userId);

        var result = await _mediator.Send(new DeleteMessageCommand(messageId, userId, deleteDto.RoomId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Get participants of a chat room
    /// </summary>
    [HttpGet("rooms/{roomId:int}/participants")]
    public async Task<IActionResult> GetRoomParticipants(int roomId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Getting participants for room: {RoomId}, User: {UserId}", roomId, userId);

        var result = await _mediator.Send(new GetRoomParticipantsQuery(roomId, userId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Update chat room details
    /// </summary>
    [HttpPut("rooms/{roomId:int}")]
    public async Task<IActionResult> UpdateChatRoom(int roomId, [FromBody] UpdateChatRoomDto updateDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Updating chat room: {RoomId}, User: {UserId}", roomId, userId);

        var result = await _mediator.Send(new UpdateChatRoomCommand(roomId, updateDto, userId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Delete or leave chat room
    /// </summary>
    [HttpDelete("rooms/{roomId:int}")]
    public async Task<IActionResult> DeleteChatRoom(int roomId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Deleting/Leaving chat room: {RoomId}, User: {UserId}", roomId, userId);

        var result = await _mediator.Send(new DeleteChatRoomCommand(roomId, userId));
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Search messages across rooms
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchMessages(
        [FromQuery] string query,
        [FromQuery] int? roomId = null,
        [FromQuery] string? type = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return BadRequest("Invalid user ID in token");

        _logger.LogInformation("Searching messages for user: {UserId}, Query: {Query}", userId, query);

        var result = await _mediator.Send(new SearchMessagesQuery(query, userId, roomId, type, fromDate, toDate, page, limit));
        return StatusCode(result.StatusCode, result);
    }

    ///// <summary>
    ///// Search chat rooms
    ///// </summary>
    //[HttpGet("rooms/search")]
    //public async Task<IActionResult> SearchRooms([FromQuery] SearchRoomsDto searchDto)
    //{
    //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
    //        return BadRequest("Invalid user ID in token");

    //    _logger.LogInformation("Searching rooms for user: {UserId}, Query: {Query}", userId, searchDto.Query);

    //    var result = await _mediator.Send(new SearchRoomsQuery(searchDto.Query, userId, searchDto.Type));
    //    return StatusCode(result.StatusCode, result);
    //}
}