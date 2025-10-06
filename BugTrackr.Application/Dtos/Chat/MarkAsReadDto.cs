namespace BugTrackr.Application.Dtos.Chat
{
    public class MarkAsReadDto
    {
        public int RoomId { get; set; }
        public int? LastMessageId { get; set; }
    }
}