using System.Net.Http.Json;
using System.Text.Json;
using AiPlatform.Application.Common.Exceptions;
using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;

namespace AiPlatform.Infrastructure.Llm;

// Google Gemini — নিজস্ব request format (contents + systemInstruction)।
public class GeminiChatProvider(IHttpClientFactory httpClientFactory, string name, ProviderConfig config)
    : IChatProvider
{
    public string Name => name;

    public async Task<ChatResult> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{config.Model}:generateContent";

        var contents = messages
            .Where(m => m.Role != "system")
            .Select(m => new
            {
                role = m.Role == "assistant" ? "model" : "user",
                parts = new[] { new { text = m.Content } }
            })
            .ToArray();

        var systemText = messages.FirstOrDefault(m => m.Role == "system")?.Content;
        object body = systemText is null
            ? new { contents }
            : new { contents, systemInstruction = new { parts = new[] { new { text = systemText } } } };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-goog-api-key", config.ApiKey);
        request.Content = JsonContent.Create(body);

        var http = httpClientFactory.CreateClient("llm");
        using var response = await http.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new LlmProviderException(Name, (int)response.StatusCode, raw);

        using var doc = JsonDocument.Parse(raw);
        var text = doc.RootElement
            .GetProperty("candidates")[0].GetProperty("content")
            .GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;

        return new ChatResult(text, Name, config.Model);
    }
}
