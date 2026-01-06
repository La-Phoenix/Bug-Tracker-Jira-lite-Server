namespace BugTrackr.Domain.Enums;

public enum NotificationType
{
    IssueAssigned = 1,
    IssueCreated = 2,
    IssueUpdated = 3,
    IssueCommented = 4,
    UserMentioned = 5,
    ProjectInvitation = 6,
    ProjectUpdate = 7,
    ChatMessage = 8,
    ChatInvitation = 9,
    ChatGroupCreated = 10,
    SystemAlert = 11,
    WeeklyDigest = 12
}
