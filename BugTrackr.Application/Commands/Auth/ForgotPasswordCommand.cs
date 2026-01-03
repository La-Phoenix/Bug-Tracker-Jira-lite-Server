using BugTrackr.Application.Common;
using BugTrackr.Application.Common.Helpers;
using BugTrackr.Application.Services;
using BugTrackr.Application.Services.Email;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace BugTrackr.Application.Commands.Auth;

public record ForgotPasswordCommand(string Email) : IRequest<ApiResponse<string>>, ISkipFluentValidation;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Valid email address is required");
    }
}

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ApiResponse<string>>
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<PasswordResetToken> _tokenRepository;
    private readonly IEmailService _emailService;
    private readonly IValidator<ForgotPasswordCommand> _validator;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    public ForgotPasswordHandler(
        IRepository<User> userRepository,
        IRepository<PasswordResetToken> tokenRepository,
        IEmailService emailService,
        IValidator<ForgotPasswordCommand> validator,
        ILogger<ForgotPasswordHandler> logger)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _emailService = emailService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
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

            var user = await _userRepository.Query()
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            // Always return success for security reasons, even if user doesn't exist
            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
                return ApiResponse<string>.SuccessResponse("If an account with that email exists, a password reset link has been sent.");
            }

            // Invalidate any existing tokens for this user
            var existingTokens = await _tokenRepository.Query()
                .Where(t => t.UserId == user.Id && t.IsUsed == false && t.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            foreach (var token in existingTokens)
            {
                token.IsUsed = true;
                _tokenRepository.Update(token);
            }

            // Generate new reset token
            var resetToken = GenerateSecureToken();
            var passwordResetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = resetToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1), // 1 hour expiry
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _tokenRepository.AddAsync(passwordResetToken);
            await _tokenRepository.SaveChangesAsync(cancellationToken);
            try
            {
                // Send reset email
                await _emailService.SendPasswordResetEmailAsync(user, resetToken);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password request for {Email}", request.Email);
            }

            _logger.LogInformation("Password reset email sent to {Email}", user.Email);
            return ApiResponse<string>.SuccessResponse("If an account with that email exists, a password reset link has been sent.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot password request for {Email}", request.Email);
            return ApiResponse<string>.Failure("An error occurred while processing your request", 500);
        }
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
