using System.Net.Mail;
using System.Xml.Linq;

namespace BugTrackr.Domain.Entities;

public class Issue
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }

    public int ReporterId { get; set; }
    public User Reporter { get; set; } = null!;

    public int? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;

    public int PriorityId { get; set; }
    public Priority Priority { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<IssueLabel> IssueLabels { get; set; } = new List<IssueLabel>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
    