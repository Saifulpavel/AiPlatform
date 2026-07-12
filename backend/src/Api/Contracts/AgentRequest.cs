namespace AiPlatform.Api.Contracts;

// Provider null/খালি হলে config-এর default (tool-capable হতে হবে)।
public record AgentRequest(string Message, string? Provider);
