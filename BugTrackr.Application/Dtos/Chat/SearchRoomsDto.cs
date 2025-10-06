using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTrackr.Application.Dtos.Chat
{
    public class SearchRoomsDto
    {
        public string Query { get; set; } = string.Empty;
        public string? Type { get; set; }
    }
}
