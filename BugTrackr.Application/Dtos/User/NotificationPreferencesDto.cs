using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTrackr.Application.Dtos.User
{
    public record NotificationPreferencesDto
    {
        public bool EmailNotifications { get; init; }
        public bool PushNotifications { get; init; }
        public bool IssueUpdates { get; init; }
        public bool WeeklyDigest { get; init; }
        public bool MentionAlerts { get; init; }
        public bool ProjectUpdates { get; init; }
        public bool CommentNotifications { get; init; }
        public bool AssignmentNotifications { get; init; }
    }
}
