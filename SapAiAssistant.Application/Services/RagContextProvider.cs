using System.Text;
using Microsoft.Extensions.Options;
using SapAiAssistant.Application.Interfaces;
using SapAiAssistant.Domain.Abstractions;

namespace SapAiAssistant.Application.Services;

public sealed class RagContextProvider : IRagContextProvider
{
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IVectorStore _vectorStore;
    private readonly RagSettings _settings;

    public RagContextProvider(
        IEmbeddingClient embeddingClient,
        IVectorStore vectorStore,
        IOptions<RagSettings> settings)
    {
        _embeddingClient = embeddingClient;
        _vectorStore = vectorStore;
        _settings = settings.Value;
    }

    public async Task<string?> GetContextAsync(string query, CancellationToken ct = default)
    {
        var queryEmbedding = await _embeddingClient.EmbedAsync(query, ct);

        var chunks = await _vectorStore.SearchAsync(
            queryEmbedding,
            _settings.TopK,
            _settings.MinSimilarityScore,
            ct);

        if (chunks.Count == 0)
            return null;

        var sb = new StringBuilder();
        sb.AppendLine("## Knowledge Base");

        foreach (var chunk in chunks)
        {
            sb.AppendLine($"[Source: {chunk.DocumentName}, chunk {chunk.ChunkIndex}]");
            sb.AppendLine(chunk.Content);
            sb.AppendLine("---");
        }

        return sb.ToString().TrimEnd();
    }
}
