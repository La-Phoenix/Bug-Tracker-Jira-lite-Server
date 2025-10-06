using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BugTrackr.Application.Dtos.Chat
{
    public class SearchMessagesResultDto
    {
        public List<ChatMessageDto> Messages { get; set; } = new();
        public List<string> Highlights { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
    }

}
