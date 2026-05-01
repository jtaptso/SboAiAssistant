using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Infrastructure.Persistence;

/// <summary>
/// In-memory conversation repository for local development.
/// Replace with EF Core/SQLite implementation in Phase 3.
/// </summary>
public sealed class InMemoryConversationRepository : IConversationRepository
{
    private readonly Dictionary<Guid, ChatSession> _store = [];

    public Task<ChatSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<IReadOnlyList<ChatSession>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ChatSession> result = _store.Values.ToList();
        return Task.FromResult(result);
    }

    public Task SaveAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        _store[session.Id] = session;
        return Task.CompletedTask;
    }
}
