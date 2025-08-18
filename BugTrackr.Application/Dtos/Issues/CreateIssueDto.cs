namespace BugTrackr.Application.DTOs.Issues;

public record CreateIssueDto(
    string Title,
    string? Description,
    int ReporterId,
    int? AssigneeId,
    int ProjectId,
    int StatusId,
    int PriorityId,
    List<int> LabelIds = null
);
