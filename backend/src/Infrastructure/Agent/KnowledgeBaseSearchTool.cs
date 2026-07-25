using System.Text.Json;
using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;

namespace AiPlatform.Infrastructure.Agent;

// ⚠️ APP-SPECIFIC DEMO TOOL — company KB (KnowledgeBaseSeed) search। keyword retriever reuse করে।
public class KnowledgeBaseSearchTool(IRetrieverResolver retrieverResolver) : ITool
{
    public string Name => "search_knowledge_base";
    public string Description =>
        "Searches the company knowledge base (return, delivery, warranty, payment, support policies) " +
        "and returns relevant passages. Use for company-policy questions. " +
        // নইলে LLM policy doc থেকে product-specific সংখ্যা "মনে করে" বলে দেয়।
        "It holds warranty policy and claim process only — never how long a particular product or " +
        "serial is covered. For that, use get_warranty. " +
        // ⚠️ ছোট model tool-এর ইংরেজি description দেখে query-ও ইংরেজিতে পাঠায়; KB বাংলায়,
        //    তাই keyword retriever একটাও শব্দ মেলাতে পারে না → খালি ফলাফল (cerebras, 2026-07-25)।
        "The knowledge base is written in Bengali — always send the query in Bengali, " +
        "using the user's own words.";

    public object ParametersSchema => new
    {
        type = "object",
        properties = new
        {
            query = new
            {
                type = "string",
                description = "The search query, in Bengali — reuse the user's wording, e.g. 'ওয়ারেন্টি বাতিলের কারণ'."
            }
        },
        required = new[] { "query" }
    };

    public async Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var query = arguments.TryGetProperty("query", out var q) ? q.GetString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(query))
            return "No query provided.";

        var keyword = retrieverResolver.Resolve("keyword");
        if (keyword is null)
            return "Knowledge base unavailable.";

        var chunks = await keyword.RetrieveAsync(query, 3, cancellationToken);

        // keyword খালি ফেরত দেওয়ার মানে সাধারণত শব্দ মেলেনি — ভিন্ন ভাষা বা ভিন্ন শব্দে লেখা প্রশ্ন।
        // তখনই কেবল hybrid (keyword + embedding) চালাই: embedding model বহুভাষী, তাই ইংরেজি
        // প্রশ্নেও বাংলা passage খুঁজে পায়। সবসময় hybrid চালালে প্রতি tool call-এ একটা embedding
        // API call যোগ হতো — সাধারণ ক্ষেত্রে অপচয়।
        if (chunks.Count == 0)
            chunks = await FallbackAsync(query, cancellationToken);

        return chunks.Count == 0
            ? "No relevant information found."
            : string.Join("\n", chunks.Select(c => c.Text));
    }

    private async Task<IReadOnlyList<RetrievedChunk>> FallbackAsync(string query, CancellationToken cancellationToken)
    {
        var hybrid = retrieverResolver.Resolve("hybrid");
        if (hybrid is null)
            return [];

        // embedding provider quota শেষ বা বন্ধ থাকতে পারে — তাতে পুরো tool ব্যর্থ হওয়ার দরকার নেই,
        // "কিছু পাইনি" বলাই যথেষ্ট (agent তখন অন্য tool বা সরাসরি উত্তরে যেতে পারে)।
        try
        {
            return await hybrid.RetrieveAsync(query, 3, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return [];
        }
    }
}
