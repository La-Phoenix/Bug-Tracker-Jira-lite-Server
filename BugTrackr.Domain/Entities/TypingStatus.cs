namespace BugTrackr.Domain.Entities;

public class TypingStatus
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public bool IsTyping { get; set; }
    public DateTime Timestamp { get; set; }

    // Navigation properties
    public ChatRoom Room { get; set; } = null!;
    public User User { get; set; } = null!;
}