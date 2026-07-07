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

    public async IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var body = new
        {
            model = config.Model,
            stream = true,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, config.BaseUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        request.Content = JsonContent.Create(body);

        var http = httpClientFactory.CreateClient("llm");
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new LlmProviderException(Name, (int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data:")) continue;
            var json = line["data:".Length..].Trim();
            if (json.Length == 0) continue;
            if (json == "[DONE]") break;

            using var doc = JsonDocument.Parse(json);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() > 0 &&
                choices[0].GetProperty("delta").TryGetProperty("content", out var content))
            {
                var text = content.GetString();
                if (!string.IsNullOrEmpty(text)) yield return text;
            }
        }
    }
}
