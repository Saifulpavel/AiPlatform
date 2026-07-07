namespace AiPlatform.Application.Common.Models;

// Provider-agnostic chat message। role: "system" | "user" | "assistant"।
public record ChatMessage(string Role, string Content);

// এক provider call-এর ফল।
public record ChatResult(string Content, string Provider, string Model);

// API-তে ফেরত চ্যাট উত্তর।
public record ChatResponse(string Answer, string Provider, string Model);
