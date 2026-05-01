using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Application.Interfaces;

public interface IPromptRenderer
{
    /// <summary>
    /// Assembles the full prompt for an LLM request by merging system instructions,
    /// mode-specific instructions, conversation memory, and the user message.
    /// </summary>
    Task<string> RenderAsync(
        AssistantMode mode,
        IReadOnlyList<(MessageRole Role, string Content)> history,
        string userMessage,
        string? sapContext = null,
        CancellationToken cancellationToken = default);
}
