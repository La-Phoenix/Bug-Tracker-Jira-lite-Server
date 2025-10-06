namespace BugTrackr.Application.Dtos.Chat;

public class SendMessageDto
{
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public int? ReplyToId { get; set; }
}