using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.User;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Users.Queries;

public record GetAllUsersQuery() : IRequest<ApiResponse<IEnumerable<UserDto>>>;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, ApiResponse<IEnumerable<UserDto>>>
{
    private readonly IRepository<User> _userRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllUsersQueryHandler> _logger;

    public GetAllUsersQueryHandler(
        IRepository<User> userRepo,
        IMapper mapper,
        ILogger<GetAllUsersQueryHandler> logger)
    {
        _userRepo = userRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var users = await _userRepo.Query()
                .Include(u => u.ProjectUsers)
                    .ThenInclude(pu => pu.Project)
                .ToListAsync(cancellationToken);

            var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);

            _logger.LogInformation("Retrieved {Count} users", users.Count);
            return ApiResponse<IEnumerable<UserDto>>.SuccessResponse(userDtos, 200, "Users retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users: {Message}", ex.Message);
            return ApiResponse<IEnumerable<UserDto>>.Failure("An unexpected error occurred.", 500);
        }
    }
}

