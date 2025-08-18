namespace BugTrackr.Application.DTOs.Issues;

public record IssueDto(
    int Id,
    string Title,
    string? Description,
    int ReporterId,
    string ReporterName,
    int? AssigneeId,
    string? AssigneeName,
    int ProjectId,
    string ProjectName,
    int StatusId,
    string StatusName,
    int PriorityId,
    string PriorityName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<string> Labels
);
