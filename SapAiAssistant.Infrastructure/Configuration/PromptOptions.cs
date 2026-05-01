namespace SapAiAssistant.Infrastructure.Configuration;

public sealed class PromptOptions
{
    public const string Section = "Prompts";

    /// <summary>Absolute or relative path to the prompts directory.</summary>
    public string TemplatesPath { get; set; } = "prompts";
}
