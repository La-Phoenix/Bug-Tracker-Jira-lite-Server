using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTrackr.Application.Dtos.User
{
    public record ChangePasswordDto
    {
        public string CurrentPassword { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
        public string ConfirmPassword { get; init; } = string.Empty;
    }
}
