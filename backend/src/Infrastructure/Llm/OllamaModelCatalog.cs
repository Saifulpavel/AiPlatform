using System.Text.Json;
using AiPlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiPlatform.Infrastructure.Llm;

// Ollama /api/tags থেকে installed model তালিকা। ollama configure না থাকলে বা down থাকলে
// খালি তালিকা (dev convenience — throw করে না, UI শুধু কম option দেখায়)।
public class OllamaModelCatalog(
    IHttpClientFactory httpClientFactory,
    IOptions<LlmSettings> options,
    ILogger<OllamaModelCatalog> logger)
    : ILocalModelCatalog
{
    private readonly LlmSettings _settings = options.Value;

    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken)
    {
        // chat BaseUrl (…:11434/v1/chat/completions) থেকে authority নিয়ে /api/tags বানাই
        if (!_settings.Providers.TryGetValue("ollama", out var ollama) || string.IsNullOrWhiteSpace(ollama.BaseUrl))
            return [];

        Uri tagsUri;
        try
        {
            var authority = new Uri(ollama.BaseUrl).GetLeftPart(UriPartial.Authority);
            tagsUri = new Uri($"{authority}/api/tags");
        }
        catch (UriFormatException)
        {
            return [];
        }

        try
        {
            var http = httpClientFactory.CreateClient("llm");
            using var response = await http.GetAsync(tagsUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return [];

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(raw);
            if (!doc.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
                return [];

            return models.EnumerateArray()
                .Select(m => m.TryGetProperty("name", out var n) ? n.GetString() : null)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                // embedding model chat-এ চলে না — বাদ। ponytail: নাম-heuristic ("embed"); নির্ভুল লাগলে
                // প্রতি model-এ /api/show করে capability দেখতে হবে (এখন দরকার নেই, একটাই embed model)।
                .Where(name => !name.Contains("embed", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // ollama down / timeout / বাজে response — খালি তালিকা, feature ভাঙে না
            logger.LogWarning(ex, "Ollama model list fetch failed ({Uri})", tagsUri);
            return [];
        }
    }
}
