using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;
using AiPlatform.Application.Common.Results;
using MediatR;

namespace AiPlatform.Application.Features.Rag;

// method-agnostic: retriever resolver method দিয়ে বাছে, তাই keyword/embedding/qdrant একই handler-এ চলে।
public class RagQueryCommandHandler(
    IRetrieverResolver retrieverResolver,
    IChatProviderResolver chatResolver)
    : IRequestHandler<RagQueryCommand, Result<RagResponse>>
{
    private const int TopN = 3;
    private const string NotFoundAnswer = "দুঃখিত, এই তথ্যটি আমার কাছে নেই।";

    // Hybrid prompt: instruction English (grounding মানা reliable), উত্তর বাংলায়, source নম্বর সহ (citation)।
    private const string SystemPrompt =
        "You are a company assistant. Answer the user's question using ONLY the provided context. " +
        "Each context item is numbered like [1], [2]. Cite the numbers you used in your answer, e.g. [1]. " +
        "If the answer is not in the context, say you do not have that information — do not make anything up. " +
        "Answer in Bengali.";

    public async Task<Result<RagResponse>> Handle(RagQueryCommand request, CancellationToken cancellationToken)
    {
        var retriever = retrieverResolver.Resolve(request.Method);
        if (retriever is null)
        {
            var requested = string.IsNullOrWhiteSpace(request.Method) ? "keyword" : request.Method;
            return Error.Validation("Rag.UnknownMethod", $"Unknown retrieval method '{requested}'.");
        }

        var chunks = await retriever.RetrieveAsync(request.Question, TopN, cancellationToken);
        if (chunks.Count == 0)
            return new RagResponse(NotFoundAnswer, chunks);

        var chat = chatResolver.Resolve(request.Provider);
        if (chat is null)
        {
            var requested = string.IsNullOrWhiteSpace(request.Provider) ? "default" : request.Provider;
            return Error.Validation("Rag.UnknownProvider", $"No configured LLM provider available for '{requested}'.");
        }

        var context = string.Join("\n", chunks.Select((c, i) => $"[{i + 1}] {c.Text}"));
        var userMessage = $"তথ্য:\n{context}\n\nপ্রশ্ন: {request.Question}";

        var result = await chat.CompleteAsync(
            [new ChatMessage("system", SystemPrompt), new ChatMessage("user", userMessage)], cancellationToken);

        return new RagResponse(result.Content, chunks);
    }
}
