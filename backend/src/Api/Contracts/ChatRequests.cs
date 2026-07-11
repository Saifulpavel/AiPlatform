namespace AiPlatform.Api.Contracts;

// Provider null/খালি হলে config-এর default। ConversationId দিলে multi-turn memory চালু।
public record ChatRequest(string Message, string? Provider, string? ConversationId = null);
