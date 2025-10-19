using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTrackr.Application.Dtos.User
{
    public record UserPreferencesDto
    {
        public string Theme { get; init; } = "system";
        public bool CompactMode { get; init; }
        public bool ReducedMotion { get; init; }
        public bool SidebarCollapsed { get; init; }
        public bool AnimationsEnabled { get; init; }
        public string FontSize { get; init; } = "medium";
    }
}
