using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Domain.ValueObjects;

/// <summary>
/// Immutable value object capturing everything needed to assemble a single LLM prompt:
/// the mode, conversation history snapshot, optional SAP data context, and the current user message.
/// </summary>
public sealed class ConversationContext
{
    public Guid SessionId { get; }
    public AssistantMode Mode { get; }
    public string UserMessage { get; }
    public IReadOnlyList<(MessageRole Role, string Content)> History { get; }
    public string? SapDataContext { get; }
    public string? RagContext { get; }

    private ConversationContext(
        Guid sessionId,
        AssistantMode mode,
        string userMessage,
        IReadOnlyList<(MessageRole, string)> history,
        string? sapDataContext,
        string? ragContext)
    {
        SessionId = sessionId;
        Mode = mode;
        UserMessage = userMessage;
        History = history;
        SapDataContext = sapDataContext;
        RagContext = ragContext;
    }

    public static ConversationContext Create(
        Guid sessionId,
        AssistantMode mode,
        string userMessage,
        IReadOnlyList<(MessageRole Role, string Content)>? history = null,
        string? sapDataContext = null,
        string? ragContext = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userMessage);
        return new ConversationContext(
            sessionId,
            mode,
            userMessage,
            history ?? [],
            sapDataContext,
            ragContext);
    }

    /// <summary>Returns a new context with SAP data appended.</summary>
    public ConversationContext WithSapContext(string sapDataContext) =>
        new(SessionId, Mode, UserMessage, History, sapDataContext, RagContext);

    /// <summary>Returns a new context with RAG context appended.</summary>
    public ConversationContext WithRagContext(string ragContext) =>
        new(SessionId, Mode, UserMessage, History, SapDataContext, ragContext);
}
