namespace AiPlatform.Application.Common.Interfaces;

// Upload → background ingestion decouple করে (in-memory channel; পরে Redis Streams/RabbitMQ swap)।
public interface IIngestionQueue
{
    ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
