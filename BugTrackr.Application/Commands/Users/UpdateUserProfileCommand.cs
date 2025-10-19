using AutoMapper;
using BugTrackr.Application.Common;
using BugTrackr.Application.Common.Helpers;
using BugTrackr.Application.Dtos.User;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Users;

public record UpdateUserProfileCommand(
    int UserId,
    string Name,
    string Email,
    string? Phone,
    string? Company,
    string? Bio,
    string? Timezone,
    string? Language
) : IRequest<ApiResponse<UserProfileDto>>, ISkipFluentValidation;

public class UpdateUserProfileHandler : IRequestHandler<UpdateUserProfileCommand, ApiResponse<UserProfileDto>>
{
    private readonly IRepository<User> _userRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateUserProfileCommand> _validator;
    private readonly ILogger<UpdateUserProfileHandler> _logger;

    public class UpdateProfileDtoValidator : AbstractValidator<UpdateUserProfileCommand>
    {
        public UpdateProfileDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required")
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Valid email is required");

            RuleFor(x => x.Phone)
                .MaximumLength(20)
                .WithMessage("Phone cannot exceed 20 characters")
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Company)
                .MaximumLength(100)
                .WithMessage("Company cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.Company));

            RuleFor(x => x.Bio)
                .MaximumLength(500)
                .WithMessage("Bio cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Bio));

            RuleFor(x => x.Language)
                .Must(lang => new[] { "en", "es", "fr", "de", "it", "pt", "ja", "ko", "zh" }.Contains(lang))
                .WithMessage("Invalid language code")
                .When(x => !string.IsNullOrEmpty(x.Language));
        }
    }

    public UpdateUserProfileHandler(
        IRepository<User> userRepository,
        IMapper mapper,
        IValidator<UpdateUserProfileCommand> validator,
        ILogger<UpdateUserProfileHandler> logger)
    {
        _userRepository = userRepository;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<UserProfileDto>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return ApiResponse<UserProfileDto>.Failure("User not found", 404);
            }

            // Run validation after confirming user doesn't exist (Manual)
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(err => err.ErrorMessage);
                var resp = ApiResponse<UserProfileDto>.Failure("Validation failed", 400);
                resp.Errors = errors;
                return resp;
            }

            // Check if email is already taken by another user
            var existingUser = await _userRepository.Query().FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != request.UserId, cancellationToken);
            if (existingUser != null)
            {
                return ApiResponse<UserProfileDto>.Failure("Email is already taken", 409);
            }


            // Update user properties
            user.Name = request.Name;
            user.Email = request.Email;
            user.Phone = request.Phone;
            user.Company = request.Company;
            user.Bio = request.Bio;
            user.Timezone = request.Timezone;
            user.Language = request.Language;

            await _userRepository.UpdateAsync(user);

            var profileDto = _mapper.Map<UserProfileDto>(user);
            return ApiResponse<UserProfileDto>.SuccessResponse(profileDto, 200, "User Profile Updated Successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for user {UserId}", request.UserId);
            return ApiResponse<UserProfileDto>.Failure("An error occurred while updating profile", 500);
        }
    }
}
