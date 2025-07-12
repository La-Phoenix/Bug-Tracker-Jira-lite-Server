

namespace BugTrackr.Domain.Entities;

public class Priority
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
