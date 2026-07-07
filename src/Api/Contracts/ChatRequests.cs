namespace AiPlatform.Api.Contracts;

// Provider null/খালি হলে config-এর default।
public record ChatRequest(string Message, string? Provider);
