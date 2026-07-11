using AiPlatform.Application.Common.Models;
using AiPlatform.Application.Common.Results;
using MediatR;

namespace AiPlatform.Application.Features.Chat;

// Provider null হলে config-এর default। ConversationId দিলে multi-turn memory চালু (UserId scope)।
public record ChatCommand(string Message, string? Provider, string? ConversationId = null, string? UserId = null)
    : IRequest<Result<ChatResponse>>;
