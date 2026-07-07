using System.Net.Http.Json;
using System.Text.Json;
using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;
using Microsoft.Extensions.Options;

namespace AiPlatform.Infrastructure.Rag;

// Qdrant REST API দিয়ে vector store (demo migrate)। interface-এর পিছনে বলে পরে অন্য DB swap করা যায়।
public class QdrantVectorStore(IHttpClientFactory httpClientFactory, IOptions<QdrantSettings> options)
    : IVectorStore
{
    private readonly QdrantSettings _settings = options.Value;
    private HttpClient Http() => httpClientFactory.CreateClient("llm");
    private string CollectionUrl => $"{_settings.Url}/collections/{_settings.Collection}";

    public async Task EnsureCollectionAsync(int dimension, CancellationToken cancellationToken)
    {
        var http = Http();

        // আছে কিনা দেখো — থাকলে আর বানিও না
        using var check = await http.GetAsync(CollectionUrl, cancellationToken);
        if (check.IsSuccessStatusCode)
            return;

        using var create = new HttpRequestMessage(HttpMethod.Put, CollectionUrl)
        {
            Content = JsonContent.Create(new { vectors = new { size = dimension, distance = "Cosine" } })
        };
        using var response = await http.SendAsync(create, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpsertAsync(IEnumerable<VectorPoint> points, CancellationToken cancellationToken)
    {
        var payload = new
        {
            points = points.Select(p => new { id = p.Id, vector = p.Vector, payload = new { text = p.Text, source = p.Source } }).ToArray()
        };

        using var request = new HttpRequestMessage(HttpMethod.Put, $"{CollectionUrl}/points?wait=true")
        {
            Content = JsonContent.Create(payload)
        };
        using var response = await Http().SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(float[] query, int topN, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{CollectionUrl}/points/query")
        {
            Content = JsonContent.Create(new { query, limit = topN, with_payload = true })
        };
        using var response = await Http().SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(raw);
        var points = doc.RootElement.GetProperty("result").GetProperty("points");

        var results = new List<RetrievedChunk>();
        foreach (var point in points.EnumerateArray())
        {
            var payload = point.GetProperty("payload");
            var text = payload.GetProperty("text").GetString() ?? string.Empty;
            var source = payload.TryGetProperty("source", out var s) ? s.GetString() : null;
            var score = point.GetProperty("score").GetDouble();
            results.Add(new RetrievedChunk(text, score, source));
        }
        return results;
    }
}
