using Microsoft.EntityFrameworkCore;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Infrastructure.Persistence;

public sealed class SqliteConversationRepository : IConversationRepository
{
    private readonly AppDbContext _db;

    public SqliteConversationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ChatSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ChatSession>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.ChatSessions
            .Include(s => s.Messages)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken);

    public async Task SaveAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        var existing = await _db.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == session.Id, cancellationToken);

        if (existing is null)
            _db.ChatSessions.Add(session);
        else
            _db.Entry(existing).CurrentValues.SetValues(session);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
