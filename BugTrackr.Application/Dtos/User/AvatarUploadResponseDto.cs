using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTrackr.Application.Dtos.User
{
    public record AvatarUploadResponseDto
    {
        public string AvatarUrl { get; init; } = string.Empty;
    }
}
