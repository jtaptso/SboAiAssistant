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
    private readonly IEnumerable<IDocumentTextExtractor> _extractors;

    public DocumentIngestionService(
        IEmbeddingClient embeddingClient,
        IVectorStore vectorStore,
        IOptions<RagSettings> settings,
        IEnumerable<IDocumentTextExtractor> extractors)
    {
        _embeddingClient = embeddingClient;
        _vectorStore = vectorStore;
        _settings = settings.Value;
        _extractors = extractors;
    }

    public async Task<Guid> IngestAsync(string name, Stream content, CancellationToken ct = default)
    {
        var extractor = _extractors.FirstOrDefault(e => e.CanHandle(name))
            ?? throw new NotSupportedException($"No text extractor registered for '{Path.GetExtension(name)}'.");

        var text = await extractor.ExtractTextAsync(content, ct);

        var documentId = Guid.NewGuid();
        var chunks = SplitIntoChunks(text, _settings.ChunkSize, _settings.ChunkOverlap);

        for (int i = 0; i < chunks.Count; i++)
        {
            var embedding = await _embeddingClient.EmbedAsync(chunks[i], ct);
            var chunk = new DocumentChunk { DocumentId = documentId, DocumentName = name, ChunkIndex = i, Content = chunks[i], Embedding = embedding };
            await _vectorStore.UpsertAsync(chunk, ct);
        }

        var document = new Document { Id = documentId, Name = name, ChunkCount = chunks.Count };
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
