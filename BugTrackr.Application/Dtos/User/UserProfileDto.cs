using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTrackr.Application.Dtos.User
{
    public record UserProfileDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? Phone { get; init; }
        public string? Company { get; init; }
        public string? Bio { get; init; }
        public string? Timezone { get; init; }
        public string? Language { get; init; }
        public string? Avatar { get; init; }
    }
}
