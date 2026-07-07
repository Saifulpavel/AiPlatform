using AiPlatform.Application.Common.Models;

namespace AiPlatform.Application.Common.Interfaces;

// এক LLM chat provider (Gemini, Groq, … পরে Ollama/vLLM)। নতুন provider = নতুন impl, কোড rewrite নয়।
public interface IChatProvider
{
    string Name { get; }
    Task<ChatResult> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken);
}
