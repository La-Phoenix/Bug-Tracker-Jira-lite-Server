using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.Notification;
using BugTrackr.Application.Services.NotificationService;
using MediatR;

namespace BugTrackr.Application.Queries.Notifications;

public record GetUserNotificationsQuery(
    int UserId,
    bool UnreadOnly = false,
    int Limit = 50
) : IRequest<ApiResponse<List<NotificationDto>>>;

public class GetUserNotificationsHandler : IRequestHandler<GetUserNotificationsQuery, ApiResponse<List<NotificationDto>>>
{
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;

    public GetUserNotificationsHandler(INotificationService notificationService, IMapper mapper)
    {
        _notificationService = notificationService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<NotificationDto>>> Handle(GetUserNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notifications = await _notificationService.GetUserNotifications(
            request.UserId,
            request.UnreadOnly,
            request.Limit);

        var notificationDtos = _mapper.Map<List<NotificationDto>>(notifications);

        return ApiResponse<List<NotificationDto>>.SuccessResponse(
            notificationDtos,
            200,
            "Notifications retrieved successfully");
    }
}