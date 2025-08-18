namespace BugTrackr.Application.Dtos.User;

public record UserDto(
    int Id,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAt
);

