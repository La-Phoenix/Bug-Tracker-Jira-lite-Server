using BugTrackr.Application.Dtos.Chat;

namespace BugTrackr.Application.Services;

public interface IChatNotificationService
{
    Task NotifyNewMessage(int roomId, ChatMessageDto message);
    Task NotifyMessageEdited(int roomId, int messageId, string newContent);
    Task NotifyMessageDeleted(int roomId, int messageId);
    Task NotifyParticipantAdded(int roomId, ChatParticipantDto participant);
    Task NotifyParticipantRemoved(int roomId, int userId);
    Task NotifyRoomUpdated(ChatRoomDto room);
    Task NotifyRoomCreated(ChatRoomDto room);
    Task NotifyRemovedFromRoom(int roomId, int participantUserId);
}
