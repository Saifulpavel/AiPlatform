using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiPlatform.Infrastructure.Persistence;

public class DocumentRepository(AppDbContext dbContext) : IDocumentRepository
{
    public async Task AddAsync(Document document, CancellationToken cancellationToken) =>
        await dbContext.Documents.AddAsync(document, cancellationToken);

    public Task<Document?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
