namespace SapAiAssistant.Infrastructure.Configuration;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public int TopK { get; set; } = 3;
    public float MinSimilarityScore { get; set; } = 0.65f;
    public int ChunkSize { get; set; } = 2000;
    public int ChunkOverlap { get; set; } = 200;
}
