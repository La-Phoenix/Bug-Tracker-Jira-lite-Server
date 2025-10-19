using Microsoft.AspNetCore.Http;

    namespace BugTrackr.Application.Dtos.User;

    public class UploadAvatarDto
    {
        public IFormFile Avatar { get; set; } = null!;
    }
