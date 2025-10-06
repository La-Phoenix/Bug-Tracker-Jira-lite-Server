using BugTrackr.Application.Dtos.Chat;

namespace BugTrackr.Application.Dtos.Chat;

public class ChatRoomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Keep as string for API compatibility
    public string? Description { get; set; }
    public string? Avatar { get; set; }
    public int? ProjectId { get; set; }
    public int CreatedBy { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsPinned { get; set; }
    public bool IsMuted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ChatParticipantDto> Participants { get; set; } = new();
    public ChatMessageDto? LastMessage { get; set; }
    public int UnreadCount { get; set; }
}