using System.Text;
using Microsoft.Extensions.Options;
using SapAiAssistant.Application.Interfaces;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Application.Services;

public sealed class DocumentIngestionService : IDocumentIngestionService
{
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IVectorStore _vectorStore;
    private readonly RagSettings _settings;

    public DocumentIngestionService(
        IEmbeddingClient embeddingClient,
        IVectorStore vectorStore,
        IOptions<RagSettings> settings)
    {
        _embeddingClient = embeddingClient;
        _vectorStore = vectorStore;
        _settings = settings.Value;
    }

    public async Task<Guid> IngestAsync(string name, Stream content, CancellationToken ct = default)
    {
        using var reader = new StreamReader(content, Encoding.UTF8);
        var text = await reader.ReadToEndAsync(ct);

        var document = Document.Create(name);
        var chunks = SplitIntoChunks(text, _settings.ChunkSize, _settings.ChunkOverlap);

        for (int i = 0; i < chunks.Count; i++)
        {
            var embedding = await _embeddingClient.EmbedAsync(chunks[i], ct);
            var chunk = DocumentChunk.Create(document.Id, name, i, chunks[i], embedding);
            await _vectorStore.UpsertAsync(chunk, ct);
        }

        document.SetChunkCount(chunks.Count);
        await _vectorStore.UpsertDocumentAsync(document, ct);

        return document.Id;
    }

    public async Task DeleteAsync(Guid documentId, CancellationToken ct = default)
    {
        await _vectorStore.DeleteByDocumentAsync(documentId, ct);
    }

    private static IReadOnlyList<string> SplitIntoChunks(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        int start = 0;

        while (start < text.Length)
        {
            int end = Math.Min(start + chunkSize, text.Length);
            chunks.Add(text[start..end]);
            start += chunkSize - overlap;
        }

        return chunks;
    }
}
