using System.Threading.Channels;
using AiPlatform.Application.Common.Interfaces;

namespace AiPlatform.Infrastructure.Ingestion;

// stdlib Channel — নতুন dep ছাড়া background queue।
// ponytail: in-memory, single-process। বড় scale-এ Redis Streams/RabbitMQ swap (interface একই)।
public class InMemoryIngestionQueue : IIngestionQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public ValueTask EnqueueAsync(Guid documentId, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(documentId, cancellationToken);

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAsync(cancellationToken);
}
