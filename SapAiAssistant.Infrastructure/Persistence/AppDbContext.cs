using Microsoft.EntityFrameworkCore;
using SapAiAssistant.Domain.Entities;
using SapAiAssistant.Infrastructure.Persistence.Configurations;

namespace SapAiAssistant.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ChatSessionConfiguration());
        modelBuilder.ApplyConfiguration(new ChatMessageConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentChunkConfiguration());
    }
}
