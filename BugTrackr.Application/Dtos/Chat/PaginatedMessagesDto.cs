using BugTrackr.Application.Dtos.Chat;

namespace BugTrackr.Application.Dtos.Chat;

public class PaginatedMessagesDto
{
    public List<ChatMessageDto> Messages { get; set; } = new();
    public PaginationDto Pagination { get; set; } = new();
}


public class PaginationDto
{
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public bool HasMore { get; set; }
}