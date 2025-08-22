// File: BugTrackr.Application/DTOs/Users/TeamMemberDto.cs
namespace BugTrackr.Application.DTOs.Users;

public class TeamMemberDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public List<SharedProjectInfo> SharedProjects { get; set; } = new();
}

public class SharedProjectInfo
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string RoleInProject { get; set; } = string.Empty;
}