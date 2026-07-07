using AiPlatform.Application.Common.Models;

namespace AiPlatform.Application.Common.Interfaces;

// Vector DB abstraction (Qdrant now; interface-এর পিছনে বলে পরে অন্য store swap করা যায়)।
public interface IVectorStore
{
    Task EnsureCollectionAsync(int dimension, CancellationToken cancellationToken);
    Task UpsertAsync(IEnumerable<VectorPoint> points, CancellationToken cancellationToken);
    Task<IReadOnlyList<RetrievedChunk>> SearchAsync(float[] query, int topN, CancellationToken cancellationToken);
}
