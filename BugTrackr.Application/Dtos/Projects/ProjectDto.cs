namespace BugTrackr.Application.DTOs.Projects;

public record ProjectDto(
    int Id,
    string Name,
    string? Description,
    int CreatedById,
    string CreatedByName,
    DateTime CreatedAt,
    int IssuesCount,
    int MembersCount
);

