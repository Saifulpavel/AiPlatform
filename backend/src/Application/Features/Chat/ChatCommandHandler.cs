using System.Diagnostics;
using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;
using AiPlatform.Application.Common.Results;
using MediatR;

namespace AiPlatform.Application.Features.Chat;

public class ChatCommandHandler(IChatProviderResolver resolver, ILlmUsageRecorder usageRecorder)
    : IRequestHandler<ChatCommand, Result<ChatResponse>>
{
    public async Task<Result<ChatResponse>> Handle(ChatCommand request, CancellationToken cancellationToken)
    {
        var provider = resolver.Resolve(request.Provider);
        if (provider is null)
        {
            var requested = string.IsNullOrWhiteSpace(request.Provider) ? "default" : request.Provider;
            return Error.Validation("Chat.UnknownProvider",
                $"No configured LLM provider available for '{requested}'. Set its API key in secrets.");
        }

        var stopwatch = Stopwatch.StartNew();
        var result = await provider.CompleteAsync(
            [new ChatMessage("user", request.Message)], cancellationToken);
        stopwatch.Stop();

        usageRecorder.Record(new LlmUsageRecord(
            "chat", result.Provider, result.Model, result.InputTokens, result.OutputTokens, stopwatch.ElapsedMilliseconds));

        return new ChatResponse(result.Content, result.Provider, result.Model);
    }
}
