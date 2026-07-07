namespace AiPlatform.Domain.Entities;

public enum DocumentStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

// আপলোড করা document — chunk→embed→Qdrant ingestion pipeline এর status track করে।
public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public int TotalChunks { get; set; }
    public int ProcessedChunks { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
