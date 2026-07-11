namespace AiPlatform.Infrastructure.Llm;

// appsettings "Llm:Routing"। complexity → cheap/strong model mapping।
public class RoutingSettings
{
    public const string SectionName = "Llm:Routing";

    public bool Enabled { get; init; }
    public string CheapProvider { get; init; } = "gemini";
    public string StrongProvider { get; init; } = "groq";

    // এর বেশি শব্দ = complex ধরা হয়।
    public int WordThreshold { get; init; } = 30;

    // এই keyword থাকলে (reasoning/depth signal) complex → strong model।
    public List<string> ComplexKeywords { get; init; } = [];
}
