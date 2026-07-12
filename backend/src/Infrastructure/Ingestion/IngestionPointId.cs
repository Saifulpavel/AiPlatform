using System.Security.Cryptography;
using System.Text;

namespace AiPlatform.Infrastructure.Ingestion;

// documentId + chunk index → deterministic Qdrant point-id (UUID)।
// একই doc আবার process হলে (at-least-once redelivery) হুবহু একই id → upsert overwrite করে,
// duplicate vector জমে না। এটাই idempotent processing-এর চাবি।
public static class IngestionPointId
{
    public static string For(Guid documentId, int chunkIndex)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"{documentId}:{chunkIndex}"));
        return new Guid(bytes).ToString();
    }
}
