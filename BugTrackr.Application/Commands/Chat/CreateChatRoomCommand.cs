using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.Chat;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using BugTrackr.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Chat;

public record CreateChatRoomCommand(CreateChatRoomDto RoomData, int CreatedBy) : IRequest<ApiResponse<ChatRoomDto>>;

public class CreateChatRoomCommandValidator : AbstractValidator<CreateChatRoomCommand>
{
    public CreateChatRoomCommandValidator()
    {
        RuleFor(x => x.CreatedBy).GreaterThan(0);

        RuleFor(x => x.RoomData.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.RoomData.Type)
            .NotEmpty()
            .Must(BeValidRoomType)
            .WithMessage("Invalid room type");

        RuleFor(x => x.RoomData.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.RoomData.Description));

        RuleFor(x => x.RoomData.ParticipantIds)
            .NotEmpty()
            .Must(list => list.All(id => id > 0))
            .WithMessage("All participant IDs must be greater than 0");
    }

    private bool BeValidRoomType(string type)
    {
        return Enum.TryParse<ChatRoomType>(type, true, out _);
    }
}

public class CreateChatRoomCommandHandler : IRequestHandler<CreateChatRoomCommand, ApiResponse<ChatRoomDto>>
{
    private readonly IRepository<ChatRoom> _roomRepository;
    private readonly IRepository<ChatParticipant> _participantRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IValidator<CreateChatRoomCommand> _validator;
    private readonly ILogger<CreateChatRoomCommandHandler> _logger;

    public CreateChatRoomCommandHandler(
        IRepository<ChatRoom> roomRepository,
        IRepository<ChatParticipant> participantRepository,
        IRepository<User> userRepository,
        IValidator<CreateChatRoomCommand> validator,
        ILogger<CreateChatRoomCommandHandler> logger)
    {
        _roomRepository = roomRepository;
        _participantRepository = participantRepository;
        _userRepository = userRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<ChatRoomDto>> Handle(CreateChatRoomCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(err => err.ErrorMessage);
                var resp = ApiResponse<ChatRoomDto>.Failure("Validation failed", 400);
                resp.Errors = errors;
                return resp;
            }

            var roomData = request.RoomData;

            var participantIds = roomData.ParticipantIds.ToList();
            if (!participantIds.Contains(request.CreatedBy))
            {
                participantIds.Add(request.CreatedBy);
            }

            var existingUsers = await _userRepository.Query()
                .Where(u => participantIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            if (existingUsers.Count != participantIds.Count)
            {
                return ApiResponse<ChatRoomDto>.Failure("Some participants don't exist", 400);
            }

            // Create chat room
            var chatRoom = new ChatRoom
            {
                Name = roomData.Name,
                Type = Enum.Parse<ChatRoomType>(roomData.Type, true),
                Description = roomData.Description,
                ProjectId = roomData.ProjectId,
                CreatedBy = request.CreatedBy,
                IsPrivate = roomData.IsPrivate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _roomRepository.AddAsync(chatRoom);
            await _roomRepository.SaveChangesAsync(cancellationToken);

            // Add participants
            var participants = participantIds.Select(userId => new ChatParticipant
            {
                RoomId = chatRoom.Id,
                UserId = userId,
                Role = userId == request.CreatedBy ? ChatParticipantRole.Admin : ChatParticipantRole.Member,
                JoinedAt = DateTime.UtcNow,
                IsPinned = false,
                IsMuted = false
            }).ToList();

            await _participantRepository.AddRangeAsync(participants);
            await _participantRepository.SaveChangesAsync(cancellationToken);

            // Load room with participants for response
            var createdRoom = await _roomRepository.Query()
                .Include(r => r.Participants)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(r => r.Id == chatRoom.Id, cancellationToken);

            var roomDto = MapToDto(createdRoom!);

            return ApiResponse<ChatRoomDto>.SuccessResponse(roomDto, 201, "Chat room created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat room");
            return ApiResponse<ChatRoomDto>.Failure("An error occurred while creating the chat room", 500);
        }
    }

    private static ChatRoomDto MapToDto(ChatRoom room)
    {
        return new ChatRoomDto
        {
            Id = room.Id,
            Name = room.Name,
            Type = room.Type.ToString(),
            Description = room.Description,
            Avatar = room.Avatar,
            ProjectId = room.ProjectId,
            CreatedBy = room.CreatedBy,
            IsPrivate = room.IsPrivate,
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt,
            Participants = room.Participants.Select(p => new ChatParticipantDto
            {
                Id = p.Id,
                RoomId = p.RoomId,
                UserId = p.UserId,
                UserName = p.User.Name,
                UserEmail = p.User.Email,
                Role = p.Role.ToString(),
                JoinedAt = p.JoinedAt,
                LastSeenAt = p.LastSeenAt,
                IsPinned = p.IsPinned,
                IsMuted = p.IsMuted
            }).ToList()
        };
    }
}