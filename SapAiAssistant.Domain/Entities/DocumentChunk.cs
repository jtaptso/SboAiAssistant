namespace SapAiAssistant.Domain.Entities;

public sealed class DocumentChunk
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid DocumentId { get; init; }
    public string DocumentName { get; init; } = string.Empty;
    public int ChunkIndex { get; init; }
    public string Content { get; init; } = string.Empty;
    public float[] Embedding { get; init; } = [];
}
