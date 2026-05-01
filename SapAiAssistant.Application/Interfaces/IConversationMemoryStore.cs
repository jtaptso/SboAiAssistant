using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Application.Interfaces;

public interface IConversationMemoryStore
{
    /// <summary>Returns the most recent N messages for context assembly.</summary>
    Task<IReadOnlyList<ChatMessage>> GetRecentAsync(Guid sessionId, int maxMessages = 10, CancellationToken cancellationToken = default);
}
