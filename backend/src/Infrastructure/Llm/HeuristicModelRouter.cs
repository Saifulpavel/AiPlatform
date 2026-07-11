using AiPlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace AiPlatform.Infrastructure.Llm;

// সহজ heuristic: লম্বা প্রশ্ন বা reasoning-keyword থাকলে strong model, নইলে cheap।
// ponytail: heuristic যথেষ্ট সস্তা+deterministic; দরকার হলে পরে cheap-LLM classifier-এ upgrade।
public class HeuristicModelRouter(IOptions<RoutingSettings> options) : IModelRouter
{
    private readonly RoutingSettings _settings = options.Value;

    public bool IsEnabled => _settings.Enabled;

    public string Route(string message)
    {
        var wordCount = message.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var isComplex = wordCount > _settings.WordThreshold
            || _settings.ComplexKeywords.Any(k => message.Contains(k, StringComparison.OrdinalIgnoreCase));

        return isComplex ? _settings.StrongProvider : _settings.CheapProvider;
    }
}
