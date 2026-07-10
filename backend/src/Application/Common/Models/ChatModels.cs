namespace AiPlatform.Application.Common.Models;

// Provider-agnostic chat message। role: "system" | "user" | "assistant"।
public record ChatMessage(string Role, string Content);

// এক provider call-এর ফল (token count observability-র জন্য, default 0 → backward-compatible)।
public record ChatResult(string Content, string Provider, string Model, int InputTokens = 0, int OutputTokens = 0);

// API-তে ফেরত চ্যাট উত্তর। Cached=true হলে semantic cache থেকে (LLM call হয়নি)।
public record ChatResponse(string Answer, string Provider, string Model, bool Cached = false);
