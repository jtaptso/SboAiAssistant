using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Domain.ValueObjects;

/// <summary>
/// Immutable value object capturing everything needed to assemble a single LLM prompt:
/// the mode, conversation history snapshot, optional SAP data context, and the current user message.
/// </summary>
public sealed class ConversationContext
{
    public Guid SessionId { get; init; }
    public AssistantMode Mode { get; init; }
    public string UserMessage { get; init; } = string.Empty;
    public IReadOnlyList<(MessageRole Role, string Content)> History { get; init; } = [];
    public string? SapDataContext { get; init; }
    public string? RagContext { get; init; }
}
