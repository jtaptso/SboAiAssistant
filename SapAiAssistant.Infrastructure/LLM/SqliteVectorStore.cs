using Microsoft.EntityFrameworkCore;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Domain.Entities;
using SapAiAssistant.Infrastructure.Persistence;

namespace SapAiAssistant.Infrastructure.LLM;

public sealed class SqliteVectorStore : IVectorStore
{
    private readonly AppDbContext _db;

    public SqliteVectorStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task UpsertDocumentAsync(Document document, CancellationToken ct = default)
    {
        var existing = await _db.Documents.FindAsync([document.Id], ct);
        if (existing is null)
            _db.Documents.Add(document);
        else
            _db.Entry(existing).CurrentValues.SetValues(document);

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpsertAsync(DocumentChunk chunk, CancellationToken ct = default)
    {
        var existing = await _db.DocumentChunks.FindAsync([chunk.Id], ct);
        if (existing is null)
            _db.DocumentChunks.Add(chunk);
        else
            _db.DocumentChunks.Update(chunk);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DocumentChunk>> SearchAsync(
        float[] queryEmbedding,
        int topK,
        float minScore,
        CancellationToken ct = default)
    {
        var allChunks = await _db.DocumentChunks.AsNoTracking().ToListAsync(ct);

        return allChunks
            .Select(c => (chunk: c, score: CosineSimilarity(queryEmbedding, c.Embedding)))
            .Where(x => x.score >= minScore)
            .OrderByDescending(x => x.score)
            .Take(topK)
            .Select(x => x.chunk)
            .ToList();
    }

    public async Task DeleteByDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        await _db.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .ExecuteDeleteAsync(ct);

        await _db.Documents
            .Where(d => d.Id == documentId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> ListDocumentsAsync(CancellationToken ct = default)
    {
        return await _db.Documents.AsNoTracking().OrderBy(d => d.UploadedAt).ToListAsync(ct);
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
            return 0f;

        float dot = 0f, normA = 0f, normB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        float denom = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        return denom == 0f ? 0f : dot / denom;
    }
}
