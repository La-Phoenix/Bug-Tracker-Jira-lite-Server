﻿namespace BugTrackr.Application.Dtos.Auth;
public class AuthResponseDto
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
