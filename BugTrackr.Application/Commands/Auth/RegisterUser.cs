using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Common.Helpers;
using BugTrackr.Application.Dtos.Auth;
using BugTrackr.Application.Exceptions;
using BugTrackr.Application.Services;
using BugTrackr.Application.Services.JWT;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
    private readonly IJwtService _jwtService;
    private readonly IMapper _mapper;
    private readonly IValidator<RegisterUserCommand> _validator;

    public RegisterUserCommandHandler(
        IRepository<User> userRepo,
        IJwtService jwtService,
        IValidator<RegisterUserCommand> validator,
        IMapper mapper)
    {
        _userRepo = userRepo;
        _jwtService = jwtService;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check for existing email
            var existingUser = await _userRepo.Query()
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            //Console.WriteLine($"Existing User: {existingUser?.Email}");
            //Console.WriteLine("🔍 Existing user check for email: " + request.Email);
            //Console.WriteLine("→ Result: " + (existingUser != null ? "Found user: " + existingUser.Email : "No match"));

            if (existingUser is not null)
                //throw new AppException("Email already exists.", 409);
                return ApiResponse<AuthResponseDto>.Failure("Email already exists.", 409);

            // Run validation after confirming user exists (Manual)
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

            await _userRepo.AddAsync(newUser);
            await _userRepo.SaveChangesAsync(cancellationToken);

            // Generate JWT token and map to response DTO
            var response = _mapper.Map<AuthResponseDto>(newUser);
            response.Token = _jwtService.GenerateToken(newUser);
            return ApiResponse<AuthResponseDto>.SuccessResponse(response, 201, "Sign up successfull.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during registration: " + ex.Message);
            var resp = ApiResponse<AuthResponseDto>.Failure("An unexpected error occurred.", 500);
            return resp;
        }
    }
}