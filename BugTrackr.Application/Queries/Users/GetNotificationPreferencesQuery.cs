using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.User;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Queries.Users;

public record GetNotificationPreferencesQuery(int UserId) : IRequest<ApiResponse<NotificationPreferencesDto>>;

public class GetNotificationPreferencesHandler : IRequestHandler<GetNotificationPreferencesQuery, ApiResponse<NotificationPreferencesDto>>
{
    private readonly IRepository<User> _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetNotificationPreferencesHandler> _logger;

    public GetNotificationPreferencesHandler(
        IRepository<User> userRepository,
        IMapper mapper,
        ILogger<GetNotificationPreferencesHandler> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<NotificationPreferencesDto>> Handle(GetNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return ApiResponse<NotificationPreferencesDto>.Failure("User not found", 404);
            }

            var preferencesDto = _mapper.Map<NotificationPreferencesDto>(user);
            return ApiResponse<NotificationPreferencesDto>.SuccessResponse(preferencesDto, 200, "Notification Fetched Successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification preferences for user {UserId}", request.UserId);
            return ApiResponse<NotificationPreferencesDto>.Failure("An error occurred while retrieving notification preferences", 500);
        }
    }
}