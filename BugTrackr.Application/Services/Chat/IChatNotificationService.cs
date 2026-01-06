using BugTrackr.Application.Dtos.Chat;

namespace BugTrackr.Application.Services.Chat;

public interface IChatNotificationService
{
    Task NotifyNewMessage(int roomId, ChatMessageDto message, CancellationToken cancellationToken);
    Task NotifyMessageEdited(int roomId, int messageId, string newContent);
    Task NotifyMessageDeleted(int roomId, int messageId);
    Task NotifyParticipantAdded(int roomId, ChatParticipantDto participant, CancellationToken cancellationToken);
    Task NotifyParticipantRemoved(int roomId, int userId);
    Task NotifyRoomUpdated(ChatRoomDto room);
    Task NotifyRoomCreated(ChatRoomDto room);
    Task NotifyRemovedFromRoom(int roomId, int participantUserId);
}
