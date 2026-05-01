using SapAiAssistant.Application.Interfaces;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Infrastructure.Memory;

public sealed class ConversationMemoryStore : IConversationMemoryStore
{
    private readonly IConversationRepository _repository;

    public ConversationMemoryStore(IConversationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ChatMessage>> GetRecentAsync(
        Guid sessionId,
        int maxMessages = 10,
        CancellationToken cancellationToken = default)
    {
        var session = await _repository.GetByIdAsync(sessionId, cancellationToken);
        if (session is null) return [];

        return session.Messages
            .TakeLast(maxMessages)
            .ToList();
    }
}
