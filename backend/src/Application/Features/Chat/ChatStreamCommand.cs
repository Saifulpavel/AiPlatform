using MediatR;

namespace AiPlatform.Application.Features.Chat;

// প্রতিটা item = একটা token chunk (SSE)। ConversationId দিলে multi-turn memory (UserId scope)।
public record ChatStreamCommand(string Message, string? Provider, string? ConversationId = null, string? UserId = null)
    : IStreamRequest<string>;
