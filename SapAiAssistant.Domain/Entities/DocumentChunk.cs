namespace SapAiAssistant.Domain.Entities;

public sealed class DocumentChunk
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public string DocumentName { get; private set; }
    public int ChunkIndex { get; private set; }
    public string Content { get; private set; }
    public float[] Embedding { get; private set; }

    private DocumentChunk() { DocumentName = string.Empty; Content = string.Empty; Embedding = []; } // EF Core

    public static DocumentChunk Create(Guid documentId, string documentName, int chunkIndex, string content, float[] embedding)
    {
        return new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            DocumentName = documentName,
            ChunkIndex = chunkIndex,
            Content = content,
            Embedding = embedding
        };
    }
}
