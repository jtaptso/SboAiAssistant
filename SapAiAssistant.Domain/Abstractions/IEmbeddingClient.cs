namespace SapAiAssistant.Domain.Abstractions;

public interface IEmbeddingClient
{
    /// <summary>Generates a vector embedding for the given text.</summary>
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
}
