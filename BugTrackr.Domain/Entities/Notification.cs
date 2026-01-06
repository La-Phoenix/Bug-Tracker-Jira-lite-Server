using BugTrackr.Domain.Enums;

namespace BugTrackr.Domain.Entities;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // Contextual data as JSON for linking to related entities
    public string? Data { get; set; } // {"issueId": 123, "projectId": 456, "roomId": 789}

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }

    // Auto-expire old notifications (optional)
    public DateTime? ExpiresAt { get; set; }

    // For grouping related notifications
    public string? GroupKey { get; set; }

    // Priority level for UI display
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;
}