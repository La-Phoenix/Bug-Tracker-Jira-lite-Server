using BugTrackr.Application.Common;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using BugTrackr.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Commands.Chat;

public record DeleteChatRoomCommand(int RoomId, int UserId) : IRequest<ApiResponse<string>>;

public class DeleteChatRoomCommandValidator : AbstractValidator<DeleteChatRoomCommand>
{
    public DeleteChatRoomCommandValidator()
    {
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}

public class DeleteChatRoomCommandHandler : IRequestHandler<DeleteChatRoomCommand, ApiResponse<string>>
{
    private readonly IRepository<ChatRoom> _roomRepository;
    private readonly IRepository<ChatParticipant> _participantRepository;
    private readonly IValidator<DeleteChatRoomCommand> _validator;
    private readonly ILogger<DeleteChatRoomCommandHandler> _logger;

    public DeleteChatRoomCommandHandler(
        IRepository<ChatRoom> roomRepository,
        IRepository<ChatParticipant> participantRepository,
        IValidator<DeleteChatRoomCommand> validator,
        ILogger<DeleteChatRoomCommandHandler> logger)
    {
        _roomRepository = roomRepository;
        _participantRepository = participantRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(DeleteChatRoomCommand request, CancellationToken cancellationToken)
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

            var room = await _roomRepository.GetByIdAsync(request.RoomId);
            if (room == null)
            {
                return ApiResponse<string>.Failure("Chat room not found", 404);
            }

            // Check if user is participant
            var userParticipant = await _participantRepository.Query()
                .FirstOrDefaultAsync(p => p.RoomId == request.RoomId && p.UserId == request.UserId, cancellationToken);

            if (userParticipant == null)
            {
                return ApiResponse<string>.Failure("You are not a participant of this chat room", 403);
            }

            // If user is just a member, they can only leave (remove themselves)
            if (userParticipant.Role == ChatParticipantRole.Member)
            {
                _participantRepository.Delete(userParticipant);
                await _participantRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User {UserId} left chat room {RoomId}", request.UserId, request.RoomId);
                return ApiResponse<string>.SuccessResponse("Left chat room successfully", 200);
            }

            // Only room admins can delete the entire room
            if (userParticipant.Role == ChatParticipantRole.Admin)
            {
                _roomRepository.Delete(room);
                await _roomRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Chat room {RoomId} deleted by admin {UserId}", request.RoomId, request.UserId);
                return ApiResponse<string>.SuccessResponse("Chat room deleted successfully", 200);
            }

            return ApiResponse<string>.Failure("Insufficient permissions", 403);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting/leaving chat room {RoomId}", request.RoomId);
            return ApiResponse<string>.Failure("An error occurred", 500);
        }
    }
}