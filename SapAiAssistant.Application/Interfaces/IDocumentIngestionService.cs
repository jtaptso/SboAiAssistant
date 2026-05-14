namespace SapAiAssistant.Application.Interfaces;

public interface IDocumentIngestionService
{
    /// <summary>Ingests a plain-text document, chunks it, embeds each chunk, and persists everything.</summary>
    /// <returns>The new document's ID.</returns>
    Task<Guid> IngestAsync(string name, Stream content, CancellationToken ct = default);

    /// <summary>Deletes a document and all its chunks from the vector store.</summary>
    Task DeleteAsync(Guid documentId, CancellationToken ct = default);
}
