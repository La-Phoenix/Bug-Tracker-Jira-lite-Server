namespace BugTrackr.Application.Dtos.Chat;
public class AddParticipantsDto
{
    public List<int> UserIds { get; set; } = new();
    public int RequesterId { get; set; }
}
