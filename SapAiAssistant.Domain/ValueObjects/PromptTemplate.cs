using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a named, versioned prompt template.
/// </summary>
public sealed class PromptTemplate
{
    public string Name { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0";
    public AssistantMode? ApplicableMode { get; init; }
}
