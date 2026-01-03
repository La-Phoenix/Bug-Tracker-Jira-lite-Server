using BugTrackr.Application.Common;
using BugTrackr.Application.Common.Helpers;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Auth;

public record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string ConfirmPassword
) : IRequest<ApiResponse<string>>, ISkipFluentValidation;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Reset token is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("Password is required")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$")
            .WithMessage("Password must be at least 8 characters long and contain at least one special character, one uppercase letter, one lowercase letter, and one number.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("Passwords do not match");
    }
}

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, ApiResponse<string>>
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<PasswordResetToken> _tokenRepository;
    private readonly IValidator<ResetPasswordCommand> _validator;
    private readonly ILogger<ResetPasswordHandler> _logger;

    public ResetPasswordHandler(
        IRepository<User> userRepository,
        IRepository<PasswordResetToken> tokenRepository,
        IValidator<ResetPasswordCommand> validator,
        ILogger<ResetPasswordHandler> logger)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(err => err.ErrorMessage);
                var resp = ApiResponse<string>.Failure("Validation failed", 400);
                resp.Errors = errors;
                return resp;
            }

            var resetToken = await _tokenRepository.Query()
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == request.Token && !t.IsUsed, cancellationToken);

            if (resetToken == null)
            {
                return ApiResponse<string>.Failure("Invalid or expired reset token", 400);
            }

            if (resetToken.ExpiresAt < DateTime.UtcNow)
            {
                return ApiResponse<string>.Failure("Reset token has expired", 400);
            }

            // Update user password
            var user = resetToken.User;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _userRepository.Update(user);

            // Mark token as used
            resetToken.IsUsed = true;
            _tokenRepository.Update(resetToken);

            await _tokenRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password successfully reset for user {UserId}", user.Id);
            return ApiResponse<string>.SuccessResponse("Password has been reset successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password with token {Token}", request.Token);
            return ApiResponse<string>.Failure("An error occurred while resetting password", 500);
        }
    }
}
