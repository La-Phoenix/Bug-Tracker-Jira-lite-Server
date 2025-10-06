using BugTrackr.Application.Common;
using BugTrackr.Application.Dtos.Chat;
using BugTrackr.Application.Services;
using BugTrackr.Domain.Entities;
using BugTrackr.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Application.Queries.Chat;

public record SearchMessagesQuery(
    string Query,
    int UserId,
    int? RoomId = null,
    string? Type = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int Limit = 20
) : IRequest<ApiResponse<SearchMessagesResultDto>>;

public class SearchMessagesQueryValidator : AbstractValidator<SearchMessagesQuery>
{
    public SearchMessagesQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.Limit).InclusiveBetween(1, 50);

        RuleFor(x => x.Type)
            .Must(BeValidMessageType)
            .When(x => !string.IsNullOrEmpty(x.Type))
            .WithMessage("Invalid message type");
    }

    private bool BeValidMessageType(string? type)
    {
        if (string.IsNullOrEmpty(type)) return true;
        return Enum.TryParse<ChatMessageType>(type, true, out _);
    }
}

public class SearchMessagesQueryHandler : IRequestHandler<SearchMessagesQuery, ApiResponse<SearchMessagesResultDto>>
{
    private readonly IRepository<ChatMessage> _messageRepository;
    private readonly IRepository<ChatParticipant> _participantRepository;
    private readonly IValidator<SearchMessagesQuery> _validator;
    private readonly ILogger<SearchMessagesQueryHandler> _logger;

    public SearchMessagesQueryHandler(
        IRepository<ChatMessage> messageRepository,
        IRepository<ChatParticipant> participantRepository,
        IValidator<SearchMessagesQuery> validator,
        ILogger<SearchMessagesQueryHandler> logger)
    {
        _messageRepository = messageRepository;
        _participantRepository = participantRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<SearchMessagesResultDto>> Handle(SearchMessagesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(err => err.ErrorMessage);
                var resp = ApiResponse<SearchMessagesResultDto>.Failure("Validation failed", 400);
                resp.Errors = errors;
                return resp;
            }

            // Get rooms user is participant of
            var userRoomIds = await _participantRepository.Query()
                .Where(p => p.UserId == request.UserId)
                .Select(p => p.RoomId)
                .ToListAsync(cancellationToken);

            if (!userRoomIds.Any())
            {
                return ApiResponse<SearchMessagesResultDto>.SuccessResponse(
                    new SearchMessagesResultDto
                    {
                        Messages = new List<ChatMessageDto>(),
                        Highlights = new List<string>(),
                        Pagination = new PaginationDto
                        {
                            Page = request.Page,
                            Limit = request.Limit,
                            Total = 0,
                            HasMore = false
                        }
                    }, 200, "No messages found");
            }

            var query = _messageRepository.Query()
                .Where(m => userRoomIds.Contains(m.RoomId))
                .Where(m => m.Content.Contains(request.Query));

            // Apply filters
            if (request.RoomId.HasValue)
            {
                query = query.Where(m => m.RoomId == request.RoomId.Value);
            }

            if (!string.IsNullOrEmpty(request.Type))
            {
                var messageType = Enum.Parse<ChatMessageType>(request.Type, true);
                query = query.Where(m => m.Type == messageType);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(m => m.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(m => m.CreatedAt <= request.ToDate.Value);
            }

            var total = await query.CountAsync(cancellationToken);

            var messages = await query
                .Include(m => m.Sender)
                .Include(m => m.Room)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((request.Page - 1) * request.Limit)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);

            var messageDtos = messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                RoomId = m.RoomId,
                SenderId = m.SenderId,
                SenderName = m.Sender.Name,
                Content = m.Content,
                Type = m.Type.ToString().ToLowerInvariant(),
                CreatedAt = m.CreatedAt,
                IsEdited = m.IsEdited,
                EditedAt = m.EditedAt
            }).ToList();

            var highlights = ExtractHighlights(request.Query, messages.Select(m => m.Content).ToList());

            var result = new SearchMessagesResultDto
            {
                Messages = messageDtos,
                Highlights = highlights,
                Pagination = new PaginationDto
                {
                    Page = request.Page,
                    Limit = request.Limit,
                    Total = total,
                    HasMore = total > request.Page * request.Limit
                }
            };

            return ApiResponse<SearchMessagesResultDto>.SuccessResponse(result, 200, "Search completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching messages for query: {Query}", request.Query);
            return ApiResponse<SearchMessagesResultDto>.Failure("An error occurred while searching messages", 500);
        }
    }

    private static List<string> ExtractHighlights(string query, List<string> contents)
    {
        var highlights = new List<string>();
        var queryWords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var content in contents)
        {
            foreach (var word in queryWords)
            {
                var index = content.ToLower().IndexOf(word);
                if (index >= 0)
                {
                    var start = Math.Max(0, index - 30);
                    var length = Math.Min(content.Length - start, 100);
                    var highlight = content.Substring(start, length);

                    if (start > 0) highlight = "..." + highlight;
                    if (start + length < content.Length) highlight += "...";

                    highlights.Add(highlight);
                    break;
                }
            }
        }

        return highlights.Distinct().Take(5).ToList();
    }
}