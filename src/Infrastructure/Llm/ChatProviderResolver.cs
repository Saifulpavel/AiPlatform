using AiPlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace AiPlatform.Infrastructure.Llm;

// config-এর provider গুলো থেকে (key আছে এমন) instance বানায়, নাম দিয়ে resolve করে।
public class ChatProviderResolver : IChatProviderResolver
{
    private readonly Dictionary<string, IChatProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _defaultProvider;

    public ChatProviderResolver(IHttpClientFactory httpClientFactory, IOptions<LlmSettings> options)
    {
        var settings = options.Value;
        _defaultProvider = settings.DefaultProvider;

        foreach (var (name, config) in settings.Providers)
        {
            // key ছাড়া provider unusable — skip (তাহলে ভুল করে select করলেও পরিষ্কার error)
            if (string.IsNullOrWhiteSpace(config.ApiKey))
                continue;

            _providers[name] = config.Type.Equals("gemini", StringComparison.OrdinalIgnoreCase)
                ? new GeminiChatProvider(httpClientFactory, name, config)
                : new OpenAiCompatibleChatProvider(httpClientFactory, name, config);
        }
    }

    public IChatProvider? Resolve(string? providerName)
    {
        var name = string.IsNullOrWhiteSpace(providerName) ? _defaultProvider : providerName;
        return _providers.TryGetValue(name, out var provider) ? provider : null;
    }
}
