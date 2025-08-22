using BugTrackr.Application.Common;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Projects;

public record RemoveUserFromProjectCommand(int ProjectId, int UserId) : IRequest<ApiResponse<string>>;

public class RemoveUserFromProjectCommandValidator : AbstractValidator<RemoveUserFromProjectCommand>
{
    public RemoveUserFromProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}

public class RemoveUserFromProjectCommandHandler : IRequestHandler<RemoveUserFromProjectCommand, ApiResponse<string>>
{
    private readonly IRepository<ProjectUser> _projectUserRepo;
    private readonly ILogger<RemoveUserFromProjectCommandHandler> _logger;

    public RemoveUserFromProjectCommandHandler(
        IRepository<ProjectUser> projectUserRepo,
        ILogger<RemoveUserFromProjectCommandHandler> logger)
    {
        _projectUserRepo = projectUserRepo;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(RemoveUserFromProjectCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var projectUser = await _projectUserRepo.Query()
                .FirstOrDefaultAsync(pu => pu.ProjectId == request.ProjectId && pu.UserId == request.UserId, cancellationToken);

            if (projectUser == null)
            {
                return ApiResponse<string>.Failure("User is not a member of this project", 404);
            }

            _projectUserRepo.Delete(projectUser);
            await _projectUserRepo.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed user {UserId} from project {ProjectId}", request.UserId, request.ProjectId);
            return ApiResponse<string>.SuccessResponse("User removed from project successfully", 200);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from project {ProjectId}: {Message}",
                request.UserId, request.ProjectId, ex.Message);
            return ApiResponse<string>.Failure("An unexpected error occurred.", 500);
        }
    }
}

