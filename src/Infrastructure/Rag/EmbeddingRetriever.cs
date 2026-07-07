using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;
using AiPlatform.Application.Common.Text;

namespace AiPlatform.Infrastructure.Rag;

// seed doc গুলো chunk → embed (একবার, lazy) → query embed → cosine similarity দিয়ে top-N।
public class EmbeddingRetriever(IEmbeddingProvider embeddingProvider) : IRetriever
{
    public string Name => "embedding";

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private List<(string Chunk, float[] Vector)>? _corpus;

    public async Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string query, int topN, CancellationToken cancellationToken)
    {
        var corpus = await EnsureCorpusAsync(cancellationToken);
        var queryVector = await embeddingProvider.EmbedAsync(query, EmbeddingTaskType.RetrievalQuery, cancellationToken);

        return corpus
            .Select(c => new RetrievedChunk(c.Chunk, VectorMath.CosineSimilarity(queryVector, c.Vector)))
            .OrderByDescending(c => c.Score)
            .Take(topN)
            .ToList();
    }

    // corpus embedding একবারই বানায় (double-checked lock)
    private async Task<List<(string Chunk, float[] Vector)>> EnsureCorpusAsync(CancellationToken cancellationToken)
    {
        if (_corpus is not null) return _corpus;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_corpus is not null) return _corpus;

            var built = new List<(string Chunk, float[] Vector)>();
            foreach (var document in KnowledgeBaseSeed.Documents)
                foreach (var chunk in TextChunker.Chunk(document))
                    built.Add((chunk, await embeddingProvider.EmbedAsync(chunk, EmbeddingTaskType.RetrievalDocument, cancellationToken)));

            _corpus = built;
            return _corpus;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
