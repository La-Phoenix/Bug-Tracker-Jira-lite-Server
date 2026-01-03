using System.ComponentModel.DataAnnotations;

namespace BugTrackr.Application.Dtos.Auth;

public class AuthResponseDto
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Company { get; set; }
    public string Language { get; set; } = "en";
    public string Theme { get; set; } = "system";
    public string FontSize { get; set; } = "medium";
    public bool AnimationsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class TestEmailDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
