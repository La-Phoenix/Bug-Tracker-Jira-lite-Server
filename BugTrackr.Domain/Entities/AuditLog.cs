
namespace BugTrackr.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public int EntityId { get; set; }

    public DateTime CreatedAt { get; set; }
}
