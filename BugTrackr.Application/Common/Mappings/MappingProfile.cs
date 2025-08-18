using AutoMapper;
using BugTrackr.Application.DTOs.Issues;
using BugTrackr.Application.DTOs.Projects;
using BugTrackr.Application.DTOs.Statuses;
using BugTrackr.Application.DTOs.Priorities;
using BugTrackr.Application.DTOs.Labels;
using BugTrackr.Application.Dtos.Auth;
using BugTrackr.Application.Commands.Auth;
using BugTrackr.Application.Issues.Commands;
using BugTrackr.Domain.Entities;
using BugTrackr.Application.Dtos.User;

namespace BugTrackr.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Issue mappings - Configure constructor mapping
        CreateMap<Issue, IssueDto>()
            .ConstructUsing(src => new IssueDto(
                src.Id,
                src.Title,
                src.Description,
                src.ReporterId,
                src.Reporter.Name,
                src.AssigneeId,
                src.Assignee != null ? src.Assignee.Name : null,
                src.ProjectId,
                src.Project.Name,
                src.StatusId,
                src.Status.Name,
                src.PriorityId,
                src.Priority.Name,
                src.CreatedAt,
                src.UpdatedAt,
                src.IssueLabels.Select(il => il.Label.Name).ToList()
            ));

        // CreateIssueCommand to Issue mapping
        CreateMap<CreateIssueCommand, Issue>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IssueLabels, opt => opt.Ignore())
            .ForMember(dest => dest.Reporter, opt => opt.Ignore())
            .ForMember(dest => dest.Assignee, opt => opt.Ignore())
            .ForMember(dest => dest.Project, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.Priority, opt => opt.Ignore())
            .ForMember(dest => dest.Comments, opt => opt.Ignore())
            .ForMember(dest => dest.Attachments, opt => opt.Ignore());

        // Auth mappings
        CreateMap<RegisterUserCommand, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => BCrypt.Net.BCrypt.HashPassword(src.Password)))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => "User"))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.ProjectUsers, opt => opt.Ignore())
            .ForMember(dest => dest.ReportedIssues, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedIssues, opt => opt.Ignore());

        CreateMap<User, AuthResponseDto>()
            .ForMember(dest => dest.Token, opt => opt.Ignore()); // Token set separately

        // User mappings
        CreateMap<User, UserDto>()
            .ConstructUsing(src => new UserDto(
                src.Id,
                src.Name,
                src.Email,
                src.Role,
                src.CreatedAt
            ));

        // Project mappings
        CreateMap<Project, ProjectDto>()
            .ConstructUsing(src => new ProjectDto(
                src.Id,
                src.Name,
                src.Description,
                src.CreatedById,
                src.CreatedBy.Name,
                src.CreatedAt,
                src.Issues.Count,
                src.ProjectUsers.Count
            ));

        // Status mappings
        CreateMap<Status, StatusDto>()
            .ConstructUsing(src => new StatusDto(
                src.Id,
                src.Name
            ));

        // Priority mappings
        CreateMap<Priority, PriorityDto>()
            .ConstructUsing(src => new PriorityDto(
                src.Id,
                src.Name
            ));

        // Label mappings
        CreateMap<Label, LabelDto>()
            .ConstructUsing(src => new LabelDto(
                src.Id,
                src.Name,
                src.Color
            ));
    }
}



