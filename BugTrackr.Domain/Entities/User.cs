namespace BugTrackr.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }

    public required string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }

    // Profile fields
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Bio { get; set; }
    public string? Timezone { get; set; }
    public string? Language { get; set; } = "en";
    public string? Avatar { get; set; }

    // Notification preferences
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public bool IssueUpdates { get; set; } = true;
    public bool WeeklyDigest { get; set; } = true;
    public bool MentionAlerts { get; set; } = true;
    public bool ProjectUpdates { get; set; } = true;
    public bool CommentNotifications { get; set; } = true;
    public bool AssignmentNotifications { get; set; } = true;

    // User preferences
    public string Theme { get; set; } = "system";
    public bool CompactMode { get; set; } = false;
    public bool ReducedMotion { get; set; } = false;
    public bool SidebarCollapsed { get; set; } = false;
    public bool AnimationsEnabled { get; set; } = true;
    public string FontSize { get; set; } = "medium";

    // 2FA fields
    public bool TwoFactorEnabled { get; set; } = false;
    public string? TwoFactorSecret { get; set; }
    public ICollection<ProjectUser> ProjectUsers { get; set; } = new List<ProjectUser>();
    public ICollection<Issue> ReportedIssues { get; set; } = new List<Issue>();
    public ICollection<Issue> AssignedIssues { get; set; } = new List<Issue>();
}   