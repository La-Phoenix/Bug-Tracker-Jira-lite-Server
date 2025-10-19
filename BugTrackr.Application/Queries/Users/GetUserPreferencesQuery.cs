using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.User;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Queries.Users;

public record GetUserPreferencesQuery(int UserId) : IRequest<ApiResponse<UserPreferencesDto>>;


public class GetUserPreferencesHandler : IRequestHandler<GetUserPreferencesQuery, ApiResponse<UserPreferencesDto>>
{
    private readonly IRepository<User> _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserPreferencesHandler> _logger;

    public GetUserPreferencesHandler(
        IRepository<User> userRepository,
        IMapper mapper,
        ILogger<GetUserPreferencesHandler> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<UserPreferencesDto>> Handle(GetUserPreferencesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return ApiResponse<UserPreferencesDto>.Failure("User not found", 404);
            }

            var preferencesDto = _mapper.Map<UserPreferencesDto>(user);
            return ApiResponse<UserPreferencesDto>.SuccessResponse(preferencesDto, 200, "User Preference fetehed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user preferences for user {UserId}", request.UserId);
            return ApiResponse<UserPreferencesDto>.Failure("An error occurred while retrieving user preferences", 500);
        }
    }
}