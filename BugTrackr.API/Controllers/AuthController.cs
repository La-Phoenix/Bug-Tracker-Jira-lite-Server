using BugTrackr.Application.Commands.Auth;
using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.Auth;
using BugTrackr.Application.Exceptions;
using BugTrackr.Application.Services.JWT;
using BugTrackr.Domain.Entities;
using BugTrackr.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BugTrackr.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<AuthController> _logger;
    private readonly IJwtService _jwt;
    private readonly BugTrackrDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        ISender mediator,
        ILogger<AuthController> logger,
        IJwtService jwt,
        BugTrackrDbContext context,
        IWebHostEnvironment environment)
    {
        _mediator = mediator;
        _logger = logger;
        _jwt = jwt;
        _context = context;
        _environment = environment;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var command = new RegisterUserCommand(dto.Name, dto.Email, dto.Password);
        var result = await _mediator.Send(command);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var command = new LoginUserCommand(dto.Email, dto.Password);
        var result = await _mediator.Send(command);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("external/{provider}")]
    [AllowAnonymous]
    public IActionResult Challenge([FromRoute] string provider, [FromQuery] string returnUrl = "/")
    {
        try
        {
            _logger.LogInformation("OAuth challenge initiated for provider: {Provider}", provider);

            // Validate provider
            if (!new[] { "Google", "GitHub" }.Contains(provider, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<string>.Failure("Invalid authentication provider", 400));
            }

            // Validate return URL to prevent open redirect attacks
            if (!IsValidReturnUrl(returnUrl))
            {
                returnUrl = _environment.IsProduction()
                    ? "https://bug-tracker-jira-lite-client.vercel.app"
                    : "http://localhost:5173";
            }

            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { returnUrl }, Request.Scheme);

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl,
                Items =
                {
                    ["LoginProvider"] = provider,
                    ["ReturnUrl"] = returnUrl
                }
            };

            return Challenge(properties, provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OAuth challenge for provider: {Provider}", provider);
            return StatusCode(500, ApiResponse<string>.Failure("Authentication error", 500));
        }
    }

    private bool IsValidReturnUrl(string returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return false;

        // Allow relative URLs
        if (returnUrl.StartsWith("/"))
            return true;

        // Check against allowed domains
        var allowedDomains = _environment.IsProduction()
            ? new[] { "https://bug-tracker-jira-lite-client.vercel.app" }
            : new[] { "http://localhost:5173", "http://localhost:3000", "http://localhost:8080" };

        return allowedDomains.Any(domain => returnUrl.StartsWith(domain, StringComparison.OrdinalIgnoreCase));
    }

    [HttpGet("external/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/")
    {
        try
        {
            _logger.LogInformation("Processing OAuth callback for return URL: {ReturnUrl}", returnUrl);

            // Check for OAuth error parameters first
            if (Request.Query.ContainsKey("error"))
            {
                var error = Request.Query["error"].ToString();
                var errorDescription = Request.Query["error_description"].ToString();
                _logger.LogError("OAuth callback error: {Error} - {Description}", error, errorDescription);

                var errorReturnUrl = GetValidReturnUrl(returnUrl);
                return Redirect($"{errorReturnUrl}?error=oauth_failed&message={Uri.EscapeDataString(error)}");
            }

            // Attempt external authentication
            var result = await HttpContext.AuthenticateAsync("External");

            if (result?.Principal == null || !result.Succeeded)
            {
                _logger.LogError("External authentication failed - Succeeded: {Succeeded}, Principal: {Principal}",
                    result?.Succeeded, result?.Principal != null ? "Present" : "Null");

                // Log more details about the failure
                if (result?.Failure != null)
                {
                    _logger.LogError("Authentication failure details: {Failure}", result.Failure.Message);
                }

                var errorReturnUrl = GetValidReturnUrl(returnUrl);
                return Redirect($"{errorReturnUrl}?error=auth_failed");
            }

            // Extract external user info
            var externalUserId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? "External User";

            _logger.LogInformation("OAuth callback claims - UserId: {UserId}, Email: {Email}, Name: {Name}",
                externalUserId, email, name);

            if (string.IsNullOrEmpty(externalUserId) || string.IsNullOrEmpty(email))
            {
                _logger.LogError("Missing required claims - ExternalUserId: {ExternalUserId}, Email: {Email}",
                    externalUserId ?? "NULL", email ?? "NULL");

                var errorReturnUrl = GetValidReturnUrl(returnUrl);
                return Redirect($"{errorReturnUrl}?error=missing_claims");
            }

            // Check if user exists in database, if not create them
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    Name = name,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    Role = "User",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created new user from external login: {Email}", email);
            }

            var jwt = _jwt.GenerateToken(user);
            _logger.LogInformation("Generated JWT token for user: {Email}", email);

            // Clean up external authentication cookie
            await HttpContext.SignOutAsync("External");

            var finalReturnUrl = GetValidReturnUrl(returnUrl);
            var redirectUrl = $"{finalReturnUrl}?token={jwt}";
            _logger.LogInformation("Redirecting to: {RedirectUrl}", redirectUrl);

            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExternalLoginCallback");
            var errorReturnUrl = GetValidReturnUrl(returnUrl);
            return Redirect($"{errorReturnUrl}?error=callback_error");
        }
    }

    private string GetValidReturnUrl(string returnUrl)
    {
        if (IsValidReturnUrl(returnUrl))
            return returnUrl;

        return _environment.IsProduction()
            ? "https://bug-tracker-jira-lite-client.vercel.app"
            : "http://localhost:5173";
    }




    //[HttpGet("debug/users")]
    //[AllowAnonymous]
    //public async Task<IActionResult> DebugUsers()
    //{
    //    var users = await _context.Users.ToListAsync();
    //    return Ok(users);
    //}

    //[HttpGet("test-exception")]
    //public IActionResult TestException()
    //{
    //    throw new AppException("This should be caught", 400);
    //}
}

