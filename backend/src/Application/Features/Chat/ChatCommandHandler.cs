using System.Diagnostics;
using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;
using AiPlatform.Application.Common.Results;
using MediatR;

namespace AiPlatform.Application.Features.Chat;

public class ChatCommandHandler(
    IChatProviderResolver resolver,
    ILlmUsageRecorder usageRecorder,
    IEmbeddingProvider embeddingProvider,
    ISemanticCache semanticCache)
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

        // semantic cache — কাছাকাছি প্রশ্ন আগে জিজ্ঞাসা হয়ে থাকলে LLM এড়িয়ে cached উত্তর
        float[]? queryEmbedding = null;
        if (semanticCache.IsEnabled)
        {
            queryEmbedding = await embeddingProvider.EmbedAsync(request.Message, EmbeddingTaskType.RetrievalQuery, cancellationToken);
            var cached = await semanticCache.GetAsync(queryEmbedding, cancellationToken);
            if (cached is not null)
                return new ChatResponse(cached, "cache", "cache", Cached: true);
        }

        var stopwatch = Stopwatch.StartNew();
        var result = await provider.CompleteAsync(
            [new ChatMessage("user", request.Message)], cancellationToken);
        stopwatch.Stop();

        usageRecorder.Record(new LlmUsageRecord(
            "chat", result.Provider, result.Model, result.InputTokens, result.OutputTokens, stopwatch.ElapsedMilliseconds));

        if (queryEmbedding is not null)
            await semanticCache.SetAsync(queryEmbedding, result.Content, cancellationToken);

        return new ChatResponse(result.Content, result.Provider, result.Model);
    }
}
