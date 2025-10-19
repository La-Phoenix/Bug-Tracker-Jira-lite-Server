using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.User;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Users;

public record UpdateNotificationPreferencesCommand(
    int UserId,
    bool EmailNotifications,
    bool PushNotifications,
    bool IssueUpdates,
    bool WeeklyDigest,
    bool MentionAlerts,
    bool ProjectUpdates,
    bool CommentNotifications,
    bool AssignmentNotifications
) : IRequest<ApiResponse<NotificationPreferencesDto>>;

public class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("Valid user ID is required");
    }
}

public class UpdateNotificationPreferencesHandler : IRequestHandler<UpdateNotificationPreferencesCommand, ApiResponse<NotificationPreferencesDto>>
{
    private readonly IRepository<User> _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateNotificationPreferencesHandler> _logger;

    public UpdateNotificationPreferencesHandler(
        IRepository<User> userRepository,
        IMapper mapper,
        ILogger<UpdateNotificationPreferencesHandler> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<NotificationPreferencesDto>> Handle(UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return ApiResponse<NotificationPreferencesDto>.Failure("User not found", 404);
            }

            // Update notification preferences
            user.EmailNotifications = request.EmailNotifications;
            user.PushNotifications = request.PushNotifications;
            user.IssueUpdates = request.IssueUpdates;
            user.WeeklyDigest = request.WeeklyDigest;
            user.MentionAlerts = request.MentionAlerts;
            user.ProjectUpdates = request.ProjectUpdates;
            user.CommentNotifications = request.CommentNotifications;
            user.AssignmentNotifications = request.AssignmentNotifications;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            var preferencesDto = _mapper.Map<NotificationPreferencesDto>(user);
            _logger.LogInformation("Updated notification preferences for user {UserId}", request.UserId);
            return ApiResponse<NotificationPreferencesDto>.SuccessResponse(preferencesDto, 200, "Notifications Preference Updated Successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences for user {UserId}", request.UserId);
            return ApiResponse<NotificationPreferencesDto>.Failure("An error occurred while updating notification preferences", 500);
        }
    }
}
