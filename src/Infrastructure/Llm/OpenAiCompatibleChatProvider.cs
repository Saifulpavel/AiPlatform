using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AiPlatform.Application.Common.Exceptions;
using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;

namespace AiPlatform.Infrastructure.Llm;

// OpenAI-compatible (Groq, Cerebras, OpenRouter, Mistral, GitHub Models) — একই format, শুধু URL/key/model ভিন্ন।
public class OpenAiCompatibleChatProvider(IHttpClientFactory httpClientFactory, string name, ProviderConfig config)
    : IChatProvider
{
    public string Name => name;

    public async Task<ChatResult> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var body = new
        {
            model = config.Model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, config.BaseUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        request.Content = JsonContent.Create(body);

        var http = httpClientFactory.CreateClient("llm");
        using var response = await http.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new LlmProviderException(Name, (int)response.StatusCode, raw);

        using var doc = JsonDocument.Parse(raw);
        var text = doc.RootElement
            .GetProperty("choices")[0].GetProperty("message")
            .GetProperty("content").GetString() ?? string.Empty;

        return new ChatResult(text, Name, config.Model);
    }
}
