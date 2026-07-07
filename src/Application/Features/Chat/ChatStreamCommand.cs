using MediatR;

namespace AiPlatform.Application.Features.Chat;

// প্রতিটা item = একটা token chunk (SSE)।
public record ChatStreamCommand(string Message, string? Provider) : IStreamRequest<string>;
