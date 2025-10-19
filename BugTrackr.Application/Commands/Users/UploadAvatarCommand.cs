using BugTrackr.Application.Common;
using BugTrackr.Application.Common.Helpers;
using BugTrackr.Application.Dtos.Auth;
using BugTrackr.Application.Dtos.User;
using BugTrackr.Application.Services;
using BugTrackr.Application.Services.Cloudinary;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Users;

public record UploadAvatarCommand(
    int UserId,
    IFormFile File
) : IRequest<ApiResponse<AvatarUploadResponseDto>>, ISkipFluentValidation;

public class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    public UploadAvatarCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("Valid user ID is required");

        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("Avatar file is required");

        RuleFor(x => x.File)
            .Must(file => file != null && file.Length > 0)
            .WithMessage("Avatar file cannot be empty")
            .When(x => x.File != null);

        RuleFor(x => x.File)
            .Must(file => file == null || file.Length <= 5 * 1024 * 1024) // 5MB limit
            .WithMessage("Avatar file size cannot exceed 5MB")
            .When(x => x.File != null);

        RuleFor(x => x.File)
            .Must(file => file == null || IsValidImageType(file.ContentType))
            .WithMessage("Avatar must be a valid image file (JPEG, PNG, GIF, WebP)")
            .When(x => x.File != null);
    }

    private static bool IsValidImageType(string contentType)
    {
        var allowedTypes = new[]
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/gif",
            "image/webp"
        };

        return allowedTypes.Contains(contentType?.ToLower());
    }
}

public class UploadAvatarHandler : IRequestHandler<UploadAvatarCommand, ApiResponse<AvatarUploadResponseDto>>
{
    private readonly IRepository<User> _userRepository;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<UploadAvatarHandler> _logger;
    public readonly IValidator<UploadAvatarCommand> _validator;

    public UploadAvatarHandler(
        IRepository<User> userRepository,
        ICloudinaryService cloudinaryService,
        IValidator<UploadAvatarCommand> validator,
        ILogger<UploadAvatarHandler> logger)
    {
        _userRepository = userRepository;
        _cloudinaryService = cloudinaryService;
        _validator = validator;
        _logger = logger;

    }

    public async Task<ApiResponse<AvatarUploadResponseDto>> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return ApiResponse<AvatarUploadResponseDto>.Failure("User not found", 404);
            }

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(err => err.ErrorMessage);
                var resp = ApiResponse<AvatarUploadResponseDto>.Failure("Validation failed", 400);
                resp.Errors = errors;
                return resp;
            }

            // Upload new avatar
            var avatarUrl = await _cloudinaryService.UploadImageAsync(request.File, "avatars");

            // Delete old avatar if exists
            if (!string.IsNullOrEmpty(user.Avatar))
            {
                var oldPublicId = ExtractPublicIdFromUrl(user.Avatar);
                if (!string.IsNullOrEmpty(oldPublicId))
                {
                    await _cloudinaryService.DeleteImageAsync(oldPublicId);
                }
            }

            // Update user avatar
            user.Avatar = avatarUrl;
            await _userRepository.UpdateAsync(user);

            var response = new AvatarUploadResponseDto { AvatarUrl = avatarUrl };
            return ApiResponse<AvatarUploadResponseDto>.SuccessResponse(response, 200, "User Avatar Uploaded Successfully.");
        }
        catch (ArgumentException ex)
        {
            return ApiResponse<AvatarUploadResponseDto>.Failure(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar for user {UserId}", request.UserId);
            return ApiResponse<AvatarUploadResponseDto>.Failure("An error occurred while uploading avatar", 500);
        }
    }

    private static string? ExtractPublicIdFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var lastSlash = path.LastIndexOf('/');
            var lastDot = path.LastIndexOf('.');

            if (lastSlash >= 0 && lastDot > lastSlash)
            {
                return path.Substring(lastSlash + 1, lastDot - lastSlash - 1);
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return null;
    }
}
