using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;
using AiPlatform.Application.Common.Text;

namespace AiPlatform.Infrastructure.Rag;

// seed chunk গুলো একবার embed + Qdrant-এ upsert (lazy), তারপর query embed → Qdrant search।
public class QdrantRetriever(IEmbeddingProvider embeddingProvider, IVectorStore vectorStore) : IRetriever
{
    public string Name => "qdrant";

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _indexed;

    public async Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string query, int topN, CancellationToken cancellationToken)
    {
        await EnsureIndexedAsync(cancellationToken);
        var queryVector = await embeddingProvider.EmbedAsync(query, EmbeddingTaskType.RetrievalQuery, cancellationToken);
        return await vectorStore.SearchAsync(queryVector, topN, cancellationToken);
    }

    private async Task EnsureIndexedAsync(CancellationToken cancellationToken)
    {
        if (_indexed) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_indexed) return;

            var chunks = KnowledgeBaseSeed.Documents.SelectMany(d => TextChunker.Chunk(d)).ToList();
            var points = new List<VectorPoint>(chunks.Count);
            foreach (var chunk in chunks)
            {
                var vector = await embeddingProvider.EmbedAsync(chunk, EmbeddingTaskType.RetrievalDocument, cancellationToken);
                points.Add(new VectorPoint(Guid.NewGuid().ToString(), vector, chunk));
            }

            await vectorStore.EnsureCollectionAsync(points[0].Vector.Length, cancellationToken);
            await vectorStore.UpsertAsync(points, cancellationToken);
            _indexed = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
