using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a named, versioned prompt template.
/// </summary>
public sealed class PromptTemplate
{
    public string Name { get; }
    public string Content { get; }
    public string Version { get; }
    public AssistantMode? ApplicableMode { get; }

    private PromptTemplate(string name, string content, string version, AssistantMode? applicableMode)
    {
        Name = name;
        Content = content;
        Version = version;
        ApplicableMode = applicableMode;
    }

    public static PromptTemplate Create(string name, string content, string version = "1.0", AssistantMode? applicableMode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return new PromptTemplate(name, content, version, applicableMode);
    }

    /// <summary>Returns true if this template applies to the given mode (or is mode-agnostic).</summary>
    public bool IsApplicableTo(AssistantMode mode) =>
        ApplicableMode is null || ApplicableMode == mode;

    public override string ToString() => $"{Name} v{Version}";
}
