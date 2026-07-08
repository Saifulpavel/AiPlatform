namespace AiPlatform.Api.Contracts;

// Method: keyword (default) | embedding | qdrant। Provider: LLM (null হলে default)।
public record RagRequest(string Question, string? Method, string? Provider);
