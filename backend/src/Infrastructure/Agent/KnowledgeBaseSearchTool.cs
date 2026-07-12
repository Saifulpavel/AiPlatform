using System.Text.Json;
using AiPlatform.Application.Common.Interfaces;

namespace AiPlatform.Infrastructure.Agent;

// ⚠️ APP-SPECIFIC DEMO TOOL — company KB (KnowledgeBaseSeed) search। keyword retriever reuse করে।
public class KnowledgeBaseSearchTool(IRetrieverResolver retrieverResolver) : ITool
{
    public string Name => "search_knowledge_base";
    public string Description =>
        "Searches the company knowledge base (return, delivery, warranty, payment, support policies) " +
        "and returns relevant passages. Use for company-policy questions.";

    public object ParametersSchema => new
    {
        type = "object",
        properties = new { query = new { type = "string", description = "The search query (a policy question)." } },
        required = new[] { "query" }
    };

    public async Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var query = arguments.TryGetProperty("query", out var q) ? q.GetString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(query))
            return "No query provided.";

        var retriever = retrieverResolver.Resolve("keyword");
        if (retriever is null)
            return "Knowledge base unavailable.";

        var chunks = await retriever.RetrieveAsync(query, 3, cancellationToken);
        return chunks.Count == 0
            ? "No relevant information found."
            : string.Join("\n", chunks.Select(c => c.Text));
    }
}
