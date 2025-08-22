using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.User;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Queries.Users;

public record GetUsersByProjectQuery(int ProjectId) : IRequest<ApiResponse<IEnumerable<UserDto>>>;

public class GetUsersByProjectQueryValidator : AbstractValidator<GetUsersByProjectQuery>
{
    public GetUsersByProjectQueryValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
    }
}

public class GetUsersByProjectQueryHandler : IRequestHandler<GetUsersByProjectQuery, ApiResponse<IEnumerable<UserDto>>>
{
    private readonly IRepository<ProjectUser> _projectUserRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUsersByProjectQueryHandler> _logger;

    public GetUsersByProjectQueryHandler(
        IRepository<ProjectUser> projectUserRepo,
        IMapper mapper,
        ILogger<GetUsersByProjectQueryHandler> logger)
    {
        _projectUserRepo = projectUserRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<UserDto>>> Handle(GetUsersByProjectQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var users = await _projectUserRepo.Query()
                .Where(pu => pu.ProjectId == request.ProjectId)
                .Include(pu => pu.User)
                .Select(pu => pu.User)
                .ToListAsync(cancellationToken);

            var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);

            _logger.LogInformation("Retrieved {Count} users for project {ProjectId}", users.Count, request.ProjectId);
            return ApiResponse<IEnumerable<UserDto>>.SuccessResponse(userDtos, 200, "Users retrieved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users for project {ProjectId}: {Message}", request.ProjectId, ex.Message);
            return ApiResponse<IEnumerable<UserDto>>.Failure("An unexpected error occurred.", 500);
        }
    }
}
