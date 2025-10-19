using BugTrackr.Application.Common;
using BugTrackr.Application.Services;
using BugTrackr.Application.Services.Cloudinary;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Users;

public record DeleteAvatarCommand(int UserId) : IRequest<ApiResponse<string>>;

public class DeleteAvatarHandler : IRequestHandler<DeleteAvatarCommand, ApiResponse<string>>
{
    private readonly IRepository<User> _userRepository;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<DeleteAvatarHandler> _logger;

    public DeleteAvatarHandler(
        IRepository<User> userRepository,
        ICloudinaryService cloudinaryService,
        ILogger<DeleteAvatarHandler> logger)
    {
        _userRepository = userRepository;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(DeleteAvatarCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return ApiResponse<string>.Failure("User not found", 404);
            }

            if (string.IsNullOrEmpty(user.Avatar))
            {
                return ApiResponse<string>.Failure("No avatar to delete", 400);
            }

            // Extract public ID from Cloudinary URL and delete from Cloudinary
            var publicId = ExtractPublicIdFromUrl(user.Avatar);
            if (!string.IsNullOrEmpty(publicId))
            {
                var deleteResult = await _cloudinaryService.DeleteImageAsync(publicId);
                if (!deleteResult)
                {
                    _logger.LogWarning("Failed to delete avatar from Cloudinary for user {UserId}", request.UserId);
                }
            }

            // Clear avatar from database regardless of Cloudinary result
            user.Avatar = null;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Avatar deleted for user {UserId}", request.UserId);
            return ApiResponse<string>.SuccessResponse("Avatar deleted successfully", 200);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting avatar for user {UserId}", request.UserId);
            return ApiResponse<string>.Failure("An error occurred while deleting avatar", 500);
        }
    }

    private static string? ExtractPublicIdFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;

            // Handle Cloudinary URL format: /v{version}/{folder}/{public_id}.{format}
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 3)
            {
                // Get the last segment and remove file extension
                var lastSegment = segments[^1];
                var lastDot = lastSegment.LastIndexOf('.');
                if (lastDot > 0)
                {
                    var publicIdPart = lastSegment[..lastDot];
                    // Include folder if exists (segments[^2])
                    return segments.Length > 3 ? $"{segments[^2]}/{publicIdPart}" : publicIdPart;
                }
            }
        }
        catch (Exception)
        {
            // Ignore parsing errors
        }

        return null;
    }
}
