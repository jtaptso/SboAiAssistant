namespace SapAiAssistant.Application.Services;

/// <summary>Application-level RAG settings, populated from configuration.</summary>
public sealed class RagSettings
{
    public int TopK { get; set; } = 3;
    public float MinSimilarityScore { get; set; } = 0.65f;
    public int ChunkSize { get; set; } = 2000;
    public int ChunkOverlap { get; set; } = 200;
}
