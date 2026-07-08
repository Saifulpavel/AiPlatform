namespace AiPlatform.Infrastructure.Llm;

// appsettings "Llm" section। ApiKey গুলো user-secrets/env থেকে (appsettings-এ নয়)।
public class LlmSettings
{
    public const string SectionName = "Llm";

    public string DefaultProvider { get; init; } = "gemini";
    public Dictionary<string, ProviderConfig> Providers { get; init; } = new();
    public EmbeddingConfig Embedding { get; init; } = new();
}

public class ProviderConfig
{
    public string Type { get; init; } = "openai";   // "gemini" | "openai"
    public string? BaseUrl { get; init; }            // openai-compatible-এর endpoint
    public string Model { get; init; } = string.Empty;
    public string? ApiKey { get; init; }             // secrets থেকে
}

public class EmbeddingConfig
{
    public string Model { get; init; } = "gemini-embedding-001";
    public string? ApiKey { get; init; }
}
