using BugTrackr.Application.Common;
using BugTrackr.Application.Common.Helpers;
using BugTrackr.Application.Dtos.User;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Auth;

public record ChangePasswordCommand(
    int UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
) : IRequest<ApiResponse<string>>, ISkipFluentValidation;

public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required")
            .NotEmpty()
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$")
            .WithMessage("Password must be at least 8 characters long and contain at least one special character, one uppercase letter, one lowercase letter, and one number.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("Passwords do not match");
    }
}


public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, ApiResponse<string>>
{
    private readonly IRepository<User> _userRepository;
    private readonly IValidator<ChangePasswordCommand> _validator;
    private readonly ILogger<ChangePasswordHandler> _logger;

    public ChangePasswordHandler(
        IRepository<User> userRepository,
        IValidator<ChangePasswordCommand> validator,
        ILogger<ChangePasswordHandler> logger)
    {
        _userRepository = userRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return ApiResponse<string>.Failure("User not found", 404);
            }

            // Run validation after confirming user doesn't exist (Manual)
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(err => err.ErrorMessage);
                var resp = ApiResponse<string>.Failure("Validation failed", 400);
                resp.Errors = errors;
                return resp;
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return ApiResponse<string>.Failure("Current password is incorrect", 400);
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password changed for user {UserId}", request.UserId);
            return ApiResponse<string>.SuccessResponse("Password changed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", request.UserId);
            return ApiResponse<string>.Failure("An error occurred while changing password", 500);
        }
    }
}