

namespace BugTrackr.Domain.Entities;

public class Label
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Color { get; set; }

    public ICollection<IssueLabel> IssueLabels { get; set; } = new List<IssueLabel>();
}


