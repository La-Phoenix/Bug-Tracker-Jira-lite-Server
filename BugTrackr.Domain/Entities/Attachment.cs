

namespace BugTrackr.Domain.Entities;

public class Attachment
{
    public int Id { get; set; }

    public int IssueId { get; set; }
    public Issue Issue { get; set; } = null!;

    public required string FilePath { get; set; }
    public DateTime UploadedAt { get; set; }
}
