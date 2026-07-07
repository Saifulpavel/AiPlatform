using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Models;
using AiPlatform.Application.Common.Text;
using AiPlatform.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiPlatform.Infrastructure.Ingestion;

// queue থেকে document নিয়ে chunk→embed→Qdrant upsert; status/progress DB-তে update করে।
public class IngestionBackgroundService(
    IServiceProvider serviceProvider,
    IIngestionQueue queue,
    IEmbeddingProvider embeddingProvider,
    IVectorStore vectorStore,
    ILogger<IngestionBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var documentId = await queue.DequeueAsync(stoppingToken);
            try
            {
                await ProcessAsync(documentId, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ingestion failed for document {DocumentId}", documentId);
                await MarkFailedAsync(documentId, ex.Message, stoppingToken);
            }
        }
    }

    private async Task ProcessAsync(Guid documentId, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

        var document = await repository.GetAsync(documentId, cancellationToken);
        if (document is null) return;

        document.Status = DocumentStatus.Processing;
        var chunks = TextChunker.Chunk(document.Content);
        document.TotalChunks = chunks.Count;
        document.ProcessedChunks = 0;
        await repository.SaveChangesAsync(cancellationToken);

        var ensured = false;
        foreach (var chunk in chunks)
        {
            var vector = await embeddingProvider.EmbedAsync(chunk, EmbeddingTaskType.RetrievalDocument, cancellationToken);
            if (!ensured)
            {
                await vectorStore.EnsureCollectionAsync(vector.Length, cancellationToken);
                ensured = true;
            }
            await vectorStore.UpsertAsync([new VectorPoint(Guid.NewGuid().ToString(), vector, chunk)], cancellationToken);

            document.ProcessedChunks++;
            await repository.SaveChangesAsync(cancellationToken);
        }

        document.Status = DocumentStatus.Completed;
        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Ingested document {DocumentId}: {Chunks} chunks", documentId, chunks.Count);
    }

    private async Task MarkFailedAsync(Guid documentId, string error, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            var document = await repository.GetAsync(documentId, cancellationToken);
            if (document is null) return;

            document.Status = DocumentStatus.Failed;
            document.ErrorMessage = error.Length > 2000 ? error[..2000] : error;
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not mark document {DocumentId} as failed", documentId);
        }
    }
}
