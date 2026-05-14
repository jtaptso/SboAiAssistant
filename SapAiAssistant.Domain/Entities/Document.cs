namespace SapAiAssistant.Domain.Entities;

public sealed class Document
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
    public int ChunkCount { get; init; }
}
