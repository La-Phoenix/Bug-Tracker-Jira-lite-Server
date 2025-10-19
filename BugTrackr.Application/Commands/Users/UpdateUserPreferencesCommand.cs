using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Common.Helpers;
using BugTrackr.Application.Dtos.User;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Users;

public record UpdateUserPreferencesCommand(
    int UserId,
    string Theme,
    bool CompactMode,
    bool ReducedMotion,
    bool SidebarCollapsed,
    bool AnimationsEnabled,
    string FontSize
) : IRequest<ApiResponse<UserPreferencesDto>>,ISkipFluentValidation;

public class UserPreferencesDtoValidator : AbstractValidator<UpdateUserPreferencesCommand>
{
    public UserPreferencesDtoValidator()
    {
        RuleFor(x => x.Theme)
            .Must(theme => new[] { "light", "dark", "system" }.Contains(theme))
            .WithMessage("Theme must be 'light', 'dark', or 'system'");

        RuleFor(x => x.FontSize)
            .Must(size => new[] { "small", "medium", "large" }.Contains(size))
            .WithMessage("Font size must be 'small', 'medium', or 'large'");
    }
}

public class UpdateUserPreferencesHandler : IRequestHandler<UpdateUserPreferencesCommand, ApiResponse<UserPreferencesDto>>
{
    private readonly IRepository<User> _userRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateUserPreferencesCommand> _validator;
    private readonly ILogger<UpdateUserPreferencesHandler> _logger;

    public UpdateUserPreferencesHandler(
        IRepository<User> userRepository,
        IMapper mapper,
        IValidator<UpdateUserPreferencesCommand> validator,
        ILogger<UpdateUserPreferencesHandler> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<UserPreferencesDto>> Handle(UpdateUserPreferencesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(err => err.ErrorMessage);
                var resp = ApiResponse<UserPreferencesDto>.Failure("Validation failed", 400);
                resp.Errors = errors;
                return resp;
            }

            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return ApiResponse<UserPreferencesDto>.Failure("User not found", 404);
            }

            // Update user preferences
            user.Theme = request.Theme;
            user.CompactMode = request.CompactMode;
            user.ReducedMotion = request.ReducedMotion;
            user.SidebarCollapsed = request.SidebarCollapsed;
            user.AnimationsEnabled = request.AnimationsEnabled;
            user.FontSize = request.FontSize;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            var preferencesDto = _mapper.Map<UserPreferencesDto>(user);
            _logger.LogInformation("Updated user preferences for user {UserId}", request.UserId);
            return ApiResponse<UserPreferencesDto>.SuccessResponse(preferencesDto, 200, "Notifications Preference Updated Successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user preferences for user {UserId}", request.UserId);
            return ApiResponse<UserPreferencesDto>.Failure("An error occurred while updating user preferences", 500);
        }
    }
}