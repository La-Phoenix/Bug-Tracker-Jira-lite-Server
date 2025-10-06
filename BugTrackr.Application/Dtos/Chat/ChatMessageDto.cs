namespace BugTrackr.Application.Dtos.Chat
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public int? ReplyToId { get; set; }
        public ChatMessageDto? ReplyToMessage { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<MessageStatusDto> MessageStatuses { get; set; } = new();
    }
}