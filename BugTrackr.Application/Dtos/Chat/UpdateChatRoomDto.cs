using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTrackr.Application.Dtos.Chat
{
    public class UpdateChatRoomDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Avatar { get; set; }
    }
}
