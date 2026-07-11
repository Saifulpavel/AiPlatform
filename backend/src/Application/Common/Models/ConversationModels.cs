namespace AiPlatform.Application.Common.Models;

// এক conversation-এর memory: পুরনো turn-গুলোর running summary (long হলে) + সাম্প্রতিক turn verbatim।
// LLM-এ পাঠানো হয়: [summary system-message] + Recent + নতুন user message।
public record ConversationState(string? Summary, IReadOnlyList<ChatMessage> Recent)
{
    public static ConversationState Empty { get; } = new(null, []);
}
