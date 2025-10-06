using BugTrackr.Domain.Enums;

namespace BugTrackr.Domain.Entities;

public class ChatMessage
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public ChatMessageType Type { get; set; } = ChatMessageType.Text;
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public int? ReplyToId { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ChatRoom Room { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public ChatMessage? ReplyToMessage { get; set; }
    public ICollection<MessageStatus> MessageStatuses { get; set; } = new List<MessageStatus>();
}
