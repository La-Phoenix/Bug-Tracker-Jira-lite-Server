

namespace BugTrackr.Domain.Entities;

public class Comment
{
    public int Id { get; set; }

    public int IssueId { get; set; }
    public Issue Issue { get; set; } = null!;

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}
