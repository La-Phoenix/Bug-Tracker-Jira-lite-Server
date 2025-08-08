using BugTrackr.Domain.Entities;

namespace BugTrackr.Application.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}