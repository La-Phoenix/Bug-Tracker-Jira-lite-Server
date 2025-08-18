namespace BugTrackr.Application.DTOs.Issues;

public record UpdateIssueDto(
    string Title,
    string? Description,
    int? AssigneeId,
    int StatusId,
    int PriorityId,
    List<int> LabelIds = null
);
