namespace BugTrackr.Application.Dtos.Chat
{
    public class EditMessageDto
    {
        public string Content { get; set; } = string.Empty;
    }

    public class DeleteMessageDto
    {
        public int RoomId { get; set; }
    }
}
