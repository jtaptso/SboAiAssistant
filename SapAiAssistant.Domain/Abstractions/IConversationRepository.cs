using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Domain.Abstractions;

public interface IConversationRepository
{
    Task<ChatSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatSession>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(ChatSession session, CancellationToken cancellationToken = default);
}
