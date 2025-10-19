using Microsoft.AspNetCore.Http;

namespace BugTrackr.Application.Services.Cloudinary;

public interface ICloudinaryService
{
    Task<string> UploadImageAsync(IFormFile file, string folder = "avatars");
    Task<bool> DeleteImageAsync(string publicId);
}