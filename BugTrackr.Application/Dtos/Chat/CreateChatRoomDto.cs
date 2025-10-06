namespace BugTrackr.Application.Dtos.Chat;

public class CreateChatRoomDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Keep as string for API compatibility
    public string? Description { get; set; }
    public int? ProjectId { get; set; }
    public List<int> ParticipantIds { get; set; } = new();
    public bool IsPrivate { get; set; }
}