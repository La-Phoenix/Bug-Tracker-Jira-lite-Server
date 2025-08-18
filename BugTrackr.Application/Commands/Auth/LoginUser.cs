using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Common.Helpers;
using BugTrackr.Application.Dtos.Auth;
using BugTrackr.Application.Services;
using BugTrackr.Application.Services.JWT;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace BugTrackr.Application.Commands.Auth;

public record LoginUserCommand(string Email, string Password) : IRequest<ApiResponse<AuthResponseDto>>, ISkipFluentValidation;

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).WithMessage("Password must be at least 8 characters");
    }
}

public class LoginUserHandler : IRequestHandler<LoginUserCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IRepository<User> _userRepo;
    private readonly IJwtService _jwtService;
    private readonly IMapper _mapper;
    private readonly IValidator<LoginUserCommand> _validator;

    public LoginUserHandler(IRepository<User> userRepo,
        IJwtService jwtService,
        IValidator<LoginUserCommand> validator,
        IMapper mapper)
    {
        _userRepo = userRepo;
        _jwtService = jwtService;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(err => err.ErrorMessage);
                var resp = ApiResponse<AuthResponseDto>.Failure("Validation failed", 400);
                resp.Errors = errors;
                return resp;
            }
            // Check for existing user
            var user = await _userRepo.Query()
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
            if (user is null)
            {
                return ApiResponse<AuthResponseDto>.Failure("Invalid email or password.", 401);
            }
            // Verify against hashed password
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return ApiResponse<AuthResponseDto>.Failure("Invalid email or password.", 401);
            }
            // Generate JWT token
            var token = _jwtService.GenerateToken(user);
            // Map to response DTO
            var response = _mapper.Map<AuthResponseDto>(user);
            response.Token = token;
            return ApiResponse<AuthResponseDto>.SuccessResponse(response, 200, "Login successful.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during Login: " + ex.Message);
            var resp = ApiResponse<AuthResponseDto>.Failure("An unexpected error occurred.", 500);
            return resp;
        }
    }
}
