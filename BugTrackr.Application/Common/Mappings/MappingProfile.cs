using AutoMapper;
using BugTrackr.Application.DTOs.Issues;
using BugTrackr.Application.DTOs.Projects;
using BugTrackr.Application.DTOs.Statuses;
using BugTrackr.Application.DTOs.Priorities;
using BugTrackr.Application.DTOs.Labels;
using BugTrackr.Application.Dtos.Auth;
using BugTrackr.Application.Commands.Auth;
using BugTrackr.Domain.Entities;
using BugTrackr.Application.Dtos.User;
using BugTrackr.Application.Commands.Issues;
using BugTrackr.Domain.Enums;
using BugTrackr.Application.Dtos.Chat;

namespace BugTrackr.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {

        //// Issue to IssueDto mapping
        //CreateMap<Issue, IssueDto>()
        //    .ConstructUsing(src => new IssueDto(
        //        src.Id,
        //        src.Title,
        //        src.Description,
        //        src.ReporterId,
        //        src.Reporter.Name,
        //        src.AssigneeId,
        //        src.Assignee != null ? src.Assignee.Name : null,
        //        src.ProjectId,
        //        src.Project.Name,
        //        src.StatusId,
        //        src.Status.Name,
        //        src.PriorityId,
        //        src.Priority.Name,
        //        src.CreatedAt,
        //        src.UpdatedAt,
        //        src.IssueLabels != null && src.IssueLabels.Any()
        //            ? src.IssueLabels.Where(il => il.Label != null).Select(il => il.Label.Name).ToList()
        //            : new List<string>()
        //    ));
        CreateMap<Issue, IssueDto>()
        .ForCtorParam("ReporterName", opt => opt.MapFrom(src => src.Reporter.Name))
        .ForCtorParam("AssigneeName", opt => opt.MapFrom(src => src.Assignee != null ? src.Assignee.Name : null))
        .ForCtorParam("ProjectName", opt => opt.MapFrom(src => src.Project.Name))
        .ForCtorParam("StatusName", opt => opt.MapFrom(src => src.Status.Name))
        .ForCtorParam("PriorityName", opt => opt.MapFrom(src => src.Priority.Name))
        .ForCtorParam("Labels", opt => opt.MapFrom(src =>
        src.IssueLabels.Select(il => il.Label).ToList()
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

        // Chat Room mappings
        CreateMap<ChatRoom, ChatRoomDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString().ToLowerInvariant()))
            .ForMember(dest => dest.IsPinned, opt => opt.Ignore())
            .ForMember(dest => dest.IsMuted, opt => opt.Ignore())
            .ForMember(dest => dest.LastMessage, opt => opt.Ignore())
            .ForMember(dest => dest.UnreadCount, opt => opt.Ignore())
            .ForMember(dest => dest.Participants, opt => opt.MapFrom(src => src.Participants));

        CreateMap<CreateChatRoomDto, ChatRoom>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => Enum.Parse<ChatRoomType>(src.Type, true)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Project, opt => opt.Ignore())
            .ForMember(dest => dest.Participants, opt => opt.Ignore())
            .ForMember(dest => dest.Messages, opt => opt.Ignore());

        // Chat Participant mappings
        CreateMap<ChatParticipant, ChatParticipantDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Name))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.IsOnline, opt => opt.Ignore());

        // Chat Message mappings
        CreateMap<ChatMessage, ChatMessageDto>()
            .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Sender.Name))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString().ToLowerInvariant()))
            .ForMember(dest => dest.ReplyToMessage, opt => opt.MapFrom(src => src.ReplyToMessage))
            .ForMember(dest => dest.MessageStatuses, opt => opt.MapFrom(src => src.MessageStatuses));

        CreateMap<SendMessageDto, ChatMessage>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => Enum.Parse<ChatMessageType>(src.Type, true)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsEdited, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.EditedAt, opt => opt.Ignore())
            .ForMember(dest => dest.RoomId, opt => opt.Ignore())
            .ForMember(dest => dest.SenderId, opt => opt.Ignore())
            .ForMember(dest => dest.FileUrl, opt => opt.Ignore())
            .ForMember(dest => dest.FileName, opt => opt.Ignore())
            .ForMember(dest => dest.FileSize, opt => opt.Ignore())
            .ForMember(dest => dest.Room, opt => opt.Ignore())
            .ForMember(dest => dest.Sender, opt => opt.Ignore())
            .ForMember(dest => dest.ReplyToMessage, opt => opt.Ignore())
            .ForMember(dest => dest.MessageStatuses, opt => opt.Ignore());

        // Message Status mappings
        CreateMap<MessageStatus, MessageStatusDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString().ToLowerInvariant()));
    }
}


