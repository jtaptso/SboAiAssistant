namespace SapAiAssistant.Infrastructure.Configuration;

public sealed class OllamaOptions
{
    public const string Section = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3";
    public int TimeoutMinutes { get; set; } = 10;
}
