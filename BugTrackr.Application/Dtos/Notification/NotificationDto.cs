using BugTrackr.Domain.Enums;

namespace BugTrackr.Application.Dtos.Notification;

public class NotificationDto
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Data { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public PriorityLevel Priority { get; set; }
    public string? GroupKey { get; set; }
}