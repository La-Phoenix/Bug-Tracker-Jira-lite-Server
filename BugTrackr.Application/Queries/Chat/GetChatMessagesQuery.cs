using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.Chat;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Queries.Chat;

public record GetChatMessagesQuery(int RoomId, int UserId, int Page = 1, int Limit = 50, DateTime? Before = null) : IRequest<ApiResponse<PaginatedMessagesDto>>;

public class GetChatMessagesQueryHandler : IRequestHandler<GetChatMessagesQuery, ApiResponse<PaginatedMessagesDto>>
{
    private readonly IRepository<ChatMessage> _messageRepository;
    private readonly IRepository<ChatParticipant> _participantRepository;
    private readonly ILogger<GetChatMessagesQueryHandler> _logger;

    public GetChatMessagesQueryHandler(
        IRepository<ChatMessage> messageRepository,
        IRepository<ChatParticipant> participantRepository,
        ILogger<GetChatMessagesQueryHandler> logger)
    {
        _messageRepository = messageRepository;
        _participantRepository = participantRepository;
        _logger = logger;
    }

    public async Task<ApiResponse<PaginatedMessagesDto>> Handle(GetChatMessagesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify user is participant
            var isParticipant = await _participantRepository.Query()
                .AnyAsync(p => p.RoomId == request.RoomId && p.UserId == request.UserId, cancellationToken);

            if (!isParticipant)
            {
                return ApiResponse<PaginatedMessagesDto>.Failure("You are not a participant of this chat room", 403);
            }

            var query = _messageRepository.Query()
                .Where(m => m.RoomId == request.RoomId);

            if (request.Before.HasValue)
            {
                query = query.Where(m => m.CreatedAt < request.Before.Value);
            }

            var total = await query.CountAsync(cancellationToken);

            var messages = await query
                .Include(m => m.Sender)
                .Include(m => m.ReplyToMessage)
                .ThenInclude(rm => rm!.Sender)
                .Include(m => m.MessageStatuses)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((request.Page - 1) * request.Limit)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);

            var messageDtos = messages.Select(MapToDto).ToList();

            var result = new PaginatedMessagesDto
            {
                Messages = messageDtos,
                Pagination = new PaginationDto
                {
                    Page = request.Page,
                    Limit = request.Limit,
                    Total = total,
                    HasMore = total > request.Page * request.Limit
                }
            };

            return ApiResponse<PaginatedMessagesDto>.SuccessResponse(result, 200, "Messages retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages for room {RoomId}", request.RoomId);
            return ApiResponse<PaginatedMessagesDto>.Failure("An error occurred while retrieving messages", 500);
        }
    }

    private static ChatMessageDto MapToDto(ChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            RoomId = message.RoomId,
            SenderId = message.SenderId,
            SenderName = message.Sender.Name,
            Content = message.Content,
            Type = message.Type.ToString(),
            FileUrl = message.FileUrl,
            FileName = message.FileName,
            FileSize = message.FileSize,
            ReplyToId = message.ReplyToId,
            ReplyToMessage = message.ReplyToMessage != null ? new ChatMessageDto
            {
                Id = message.ReplyToMessage.Id,
                Content = message.ReplyToMessage.Content,
                SenderName = message.ReplyToMessage.Sender.Name,
                CreatedAt = message.ReplyToMessage.CreatedAt
            } : null,
            IsEdited = message.IsEdited,
            EditedAt = message.EditedAt,
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            MessageStatuses = message.MessageStatuses.Select(ms => new MessageStatusDto
            {
                Id = ms.Id,
                MessageId = ms.MessageId,
                UserId = ms.UserId,
                Status = ms.Status.ToString(),
                Timestamp = ms.Timestamp
            }).ToList()
        };
    }
}