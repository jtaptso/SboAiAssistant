namespace SapAiAssistant.Infrastructure.Configuration;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public string Model { get; set; } = "nomic-embed-text";
    public string BaseUrl { get; set; } = "http://localhost:11434";
}
