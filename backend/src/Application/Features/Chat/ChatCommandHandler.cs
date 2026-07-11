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
    ISemanticCache semanticCache,
    IModelRouter modelRouter,
    IPromptGuard promptGuard,
    IConversationStore conversationStore)
    : IRequestHandler<ChatCommand, Result<ChatResponse>>
{
    // পুরনো turn compress করার instruction (English), summary content বাংলায় হতে পারে।
    private const string SummarizePrompt =
        "You maintain a running summary of a conversation. Given the previous summary (may be empty) and the " +
        "new messages below, produce an updated concise summary that preserves key facts, names, and decisions. " +
        "Return only the summary text. Write it in Bengali.";

    public async Task<Result<ChatResponse>> Handle(ChatCommand request, CancellationToken cancellationToken)
    {
        // guardrails — LLM-এ যাওয়ার আগে: injection block, PII redact
        var message = request.Message;
        if (promptGuard.IsEnabled)
        {
            var guard = promptGuard.Check(message);
            if (guard.Blocked)
                return Error.Validation("Chat.Blocked", guard.Reason!);
            message = guard.SafeInput;
        }

        // provider explicit না দিলে + routing on → complexity দেখে cheap/strong auto-select।
        var providerName = request.Provider;
        if (string.IsNullOrWhiteSpace(providerName) && modelRouter.IsEnabled)
            providerName = modelRouter.Route(message);

        var provider = resolver.Resolve(providerName);
        if (provider is null)
        {
            var requested = string.IsNullOrWhiteSpace(request.Provider) ? "default" : request.Provider;
            return Error.Validation("Chat.UnknownProvider",
                $"No configured LLM provider available for '{requested}'. Set its API key in secrets.");
        }

        var useMemory = conversationStore.IsEnabled
            && !string.IsNullOrWhiteSpace(request.ConversationId)
            && !string.IsNullOrWhiteSpace(request.UserId);

        // semantic cache — কেবল single-turn-এ (memory থাকলে উত্তর context-নির্ভর, cache ভুল হবে)।
        float[]? queryEmbedding = null;
        if (semanticCache.IsEnabled && !useMemory)
        {
            queryEmbedding = await embeddingProvider.EmbedAsync(message, EmbeddingTaskType.RetrievalQuery, cancellationToken);
            var cached = await semanticCache.GetAsync(queryEmbedding, cancellationToken);
            if (cached is not null)
                return new ChatResponse(cached, "cache", "cache", Cached: true);
        }

        // conversation memory — summary + সাম্প্রতিক turn LLM-এ পাঠাই
        var state = ConversationState.Empty;
        var messages = new List<ChatMessage>();
        if (useMemory)
        {
            state = await conversationStore.GetAsync(request.UserId!, request.ConversationId!, cancellationToken);
            if (!string.IsNullOrWhiteSpace(state.Summary))
                messages.Add(new ChatMessage("system", $"Summary of the conversation so far: {state.Summary}"));
            messages.AddRange(state.Recent);
        }
        messages.Add(new ChatMessage("user", message));

        var stopwatch = Stopwatch.StartNew();
        var result = await provider.CompleteAsync(messages, cancellationToken);
        stopwatch.Stop();

        usageRecorder.Record(new LlmUsageRecord(
            "chat", result.Provider, result.Model, result.InputTokens, result.OutputTokens, stopwatch.ElapsedMilliseconds));

        if (queryEmbedding is not null)
            await semanticCache.SetAsync(queryEmbedding, result.Content, cancellationToken);

        if (useMemory)
            await PersistTurnAsync(request, provider, state, message, result.Content, cancellationToken);

        return new ChatResponse(result.Content, result.Provider, result.Model,
            ConversationId: useMemory ? request.ConversationId : null);
    }

    // নতুন turn history-তে যোগ; MaxRecent ছাড়ালে পুরনোগুলো summarize করে trim।
    private async Task PersistTurnAsync(
        ChatCommand request, IChatProvider provider, ConversationState state,
        string userMessage, string assistantReply, CancellationToken cancellationToken)
    {
        var recent = new List<ChatMessage>(state.Recent)
        {
            new("user", userMessage),
            new("assistant", assistantReply),
        };

        var summary = state.Summary;
        var max = conversationStore.MaxRecentMessages;
        if (recent.Count > max)
        {
            var overflow = recent.Take(recent.Count - max).ToList();
            summary = await SummarizeAsync(provider, summary, overflow, cancellationToken);
            recent = recent.Skip(recent.Count - max).ToList();
        }

        await conversationStore.SaveAsync(
            request.UserId!, request.ConversationId!, new ConversationState(summary, recent), cancellationToken);
    }

    private async Task<string> SummarizeAsync(
        IChatProvider provider, string? previousSummary, IReadOnlyList<ChatMessage> overflow, CancellationToken cancellationToken)
    {
        var block = string.Join("\n", overflow.Select(m => $"{m.Role}: {m.Content}"));
        var user = $"Previous summary:\n{previousSummary ?? "(none)"}\n\nNew messages:\n{block}";

        var stopwatch = Stopwatch.StartNew();
        var result = await provider.CompleteAsync(
            [new ChatMessage("system", SummarizePrompt), new ChatMessage("user", user)], cancellationToken);
        stopwatch.Stop();

        usageRecorder.Record(new LlmUsageRecord(
            "chat-summary", result.Provider, result.Model, result.InputTokens, result.OutputTokens, stopwatch.ElapsedMilliseconds));

        return result.Content;
    }
}
