namespace BugTrackr.Application.Dtos.Chat;

public class ChatParticipantDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsPinned { get; set; }
    public bool IsMuted { get; set; }
    public bool IsOnline { get; set; }
}