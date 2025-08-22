using System;
using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.User;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Queries.Users;

public record GetUserByIdQuery(int Id) : IRequest<ApiResponse<UserDto>>;

public class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, ApiResponse<UserDto>>
{
    private readonly IRepository<User> _userRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserByIdQueryHandler> _logger;

    public GetUserByIdQueryHandler(
        IRepository<User> userRepo,
        IMapper mapper,
        ILogger<GetUserByIdQueryHandler> logger)
    {
        _userRepo = userRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepo.GetByIdAsync(request.Id);

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", request.Id);
                return ApiResponse<UserDto>.Failure($"User with ID {request.Id} not found", 404);
            }

            var userDto = _mapper.Map<UserDto>(user);

            _logger.LogInformation("Retrieved user with ID: {UserId}", request.Id);
            return ApiResponse<UserDto>.SuccessResponse(userDto, 200, "User retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}: {Message}", request.Id, ex.Message);
            return ApiResponse<UserDto>.Failure("An unexpected error occurred.", 500);
        }
    }
}
