using BugTrackr.Domain.Entities;

namespace BugTrackr.Application.Services.JWT
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}