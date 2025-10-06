using BugTrackr.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BugTrackr.Domain.Entities;

public class ChatRoom
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public ChatRoomType Type { get; set; }

    public string? Description { get; set; }
    public string? Avatar { get; set; }
    public int? ProjectId { get; set; }
    public int CreatedBy { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
    public Project? Project { get; set; }
    public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}