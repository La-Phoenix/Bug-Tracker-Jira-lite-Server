using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.User;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Queries.Users;

public record GetUserProfileQuery(int UserId) : IRequest<ApiResponse<UserProfileDto>>;

public class GetUserProfileHandler : IRequestHandler<GetUserProfileQuery, ApiResponse<UserProfileDto>>
{
    private readonly IRepository<User> _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserProfileHandler> _logger;

    public GetUserProfileHandler(
        IRepository<User> userRepository,
        IMapper mapper,
        ILogger<GetUserProfileHandler> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return ApiResponse<UserProfileDto>.Failure("User not found", 404);
            }

            var profileDto = _mapper.Map<UserProfileDto>(user);
            return ApiResponse<UserProfileDto>.SuccessResponse(profileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile for user {UserId}", request.UserId);
            return ApiResponse<UserProfileDto>.Failure("An error occurred while retrieving user profile", 500);
        }
    }
}

