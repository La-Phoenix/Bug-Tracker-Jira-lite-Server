using AutoMapper;
using BugTrackr.Application.Auth.Commands;
using BugTrackr.Application.Dtos;
using BugTrackr.Domain.Entities;

namespace BugTrackr.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<RegisterUserCommand, User>()
            .ForMember(dest => dest.PasswordHash,
                       opt => opt.MapFrom(src => BCrypt.Net.BCrypt.HashPassword(src.Password)))
            .ForMember(dest => dest.CreatedAt,
                       opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Role,
                       opt => opt.MapFrom(_ => "User")); // default role
        CreateMap<User, AuthResponseDto>()
            .ForMember(dest => dest.Token, opt => opt.Ignore()); // Token isn't in User
    }
}
