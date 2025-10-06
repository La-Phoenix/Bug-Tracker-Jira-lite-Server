using BugTrackr.Domain.Enums;

namespace BugTrackr.Domain.Entities;

public class ChatParticipant
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public ChatParticipantRole Role { get; set; } = ChatParticipantRole.Member;
    public DateTime JoinedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsPinned { get; set; }
    public bool IsMuted { get; set; }

    // Navigation properties
    public ChatRoom Room { get; set; } = null!;
    public User User { get; set; } = null!;
}