using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Common.Helpers;
using BugTrackr.Application.Dtos.Auth;
using BugTrackr.Application.Services;
using BugTrackr.Application.Services.Email;
using BugTrackr.Application.Services.JWT;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Auth;

public record RegisterUserCommand(string Name, string Email, string Password)
    : IRequest<ApiResponse<AuthResponseDto>>, ISkipFluentValidation;


public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password)
        .NotEmpty()
        .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$")
        .WithMessage("Password must be at least 8 characters long and contain at least one special character, one uppercase letter, one lowercase letter, and one number.");

    }
}

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Project> _projectRepo;
    private readonly IRepository<ProjectUser> _projectUserRepo;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly IValidator<RegisterUserCommand> _validator;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IRepository<User> userRepo,
        IRepository<Project> projectRepo,
        IRepository<ProjectUser> projectUserRepo,
        IJwtService jwtService,
        IValidator<RegisterUserCommand> validator,
        IEmailService emailService,
        ILogger<RegisterUserCommandHandler> logger,
        IMapper mapper)
    {
        _userRepo = userRepo;
        _projectUserRepo = projectUserRepo;
        _projectRepo = projectRepo;
        _jwtService = jwtService;
        _emailService = emailService;
        _mapper = mapper;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check for existing email
            var existingUser = await _userRepo.Query()
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (existingUser is not null)
                return ApiResponse<AuthResponseDto>.Failure("Email already exists.", 409);

            // Run validation after confirming user doesn't exist (Manual)
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(err => err.ErrorMessage);
                var resp = ApiResponse<AuthResponseDto>.Failure("Validation failed", 400);
                resp.Errors = errors;
                return resp;
            }

            // Map request to User entity
            var newUser = _mapper.Map<User>(request);

            // REMOVE THE DUPLICATE - Only add once
            await _userRepo.AddAsync(newUser);
            await _userRepo.SaveChangesAsync(cancellationToken);

            // AUTO-ADD TO DEFAULT PROJECT
            var defaultProject = await _projectRepo.Query()
                .FirstOrDefaultAsync(p => p.Name == "Sample Project", cancellationToken);

            if (defaultProject != null)
            {
                var projectUser = new ProjectUser
                {
                    ProjectId = defaultProject.Id,
                    UserId = newUser.Id,
                    RoleInProject = "Member"
                };

                await _projectUserRepo.AddAsync(projectUser);
                await _projectUserRepo.SaveChangesAsync(cancellationToken);
            }

            // Send Welcome Email
            try
            {
                await _emailService.SendWelcomeEmailAsync(newUser);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", newUser.Email);
            }

            // Generate JWT token and map to response DTO
            var response = _mapper.Map<AuthResponseDto>(newUser);
            response.Token = _jwtService.GenerateToken(newUser);
            return ApiResponse<AuthResponseDto>.SuccessResponse(response, 201, "Sign up successful.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during registration: " + ex.Message);
            var resp = ApiResponse<AuthResponseDto>.Failure("An unexpected error occurred.", 500);
            return resp;
        }
    }

}