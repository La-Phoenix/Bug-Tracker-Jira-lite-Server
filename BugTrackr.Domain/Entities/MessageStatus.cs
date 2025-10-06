using BugTrackr.Domain.Enums;

namespace BugTrackr.Domain.Entities;

public class MessageStatus
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public int UserId { get; set; }
    public MessageStatusType Status { get; set; } = MessageStatusType.Sent;
    public DateTime Timestamp { get; set; }

    // Navigation properties
    public ChatMessage Message { get; set; } = null!;
    public User User { get; set; } = null!;
}