using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Domain.Abstractions;

public interface IVectorStore
{
    Task UpsertAsync(DocumentChunk chunk, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentChunk>> SearchAsync(float[] queryEmbedding, int topK, float minScore, CancellationToken ct = default);
    Task DeleteByDocumentAsync(Guid documentId, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> ListDocumentsAsync(CancellationToken ct = default);
    Task UpsertDocumentAsync(Document document, CancellationToken ct = default);
}
