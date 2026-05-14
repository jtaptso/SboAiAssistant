namespace SapAiAssistant.Domain.Entities;

public sealed class Document
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public int ChunkCount { get; private set; }

    private Document() { Name = string.Empty; } // EF Core

    public static Document Create(string name)
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            Name = name,
            UploadedAt = DateTime.UtcNow,
            ChunkCount = 0
        };
    }

    public void SetChunkCount(int count) => ChunkCount = count;
}
