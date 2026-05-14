using Microsoft.EntityFrameworkCore;
using SapAiAssistant.Domain.Entities;
using SapAiAssistant.Infrastructure.Persistence;
using FluentAssertions;

namespace SapAiAssistant.Tests.Integration.Persistence;

/// <summary>
/// Integration tests that exercise the real EF Core/SQLite stack using an in-memory
/// SQLite database (each test gets a fresh database via a unique connection string).
/// </summary>
public sealed class SqliteConversationRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly SqliteConversationRepository _sut;

    public SqliteConversationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _db  = new AppDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        _sut = new SqliteConversationRepository(_db);
    }

    // ── SaveAsync / GetByIdAsync ──────────────────────────────────────────

    [Fact]
    public async Task SaveAndGetById_RoundTrips_SessionWithMessages()
    {
        var session = new ChatSession { Mode = AssistantMode.BusinessUser, Title = "Test session" };
        session.Messages.Add(new ChatMessage { SessionId = session.Id, Role = MessageRole.User, Content = "Hello" });
        session.Messages.Add(new ChatMessage { SessionId = session.Id, Role = MessageRole.Assistant, Content = "Hi there", IsGroundedBySap = true });

        await _sut.SaveAsync(session);

        var loaded = await _sut.GetByIdAsync(session.Id);

        loaded.Should().NotBeNull();
        loaded!.Title.Should().Be("Test session");
        loaded.Mode.Should().Be(AssistantMode.BusinessUser);
        loaded.Messages.Should().HaveCount(2);
        loaded.Messages[1].IsGroundedBySap.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllSavedSessions()
    {
        var s1 = new ChatSession { Mode = AssistantMode.BusinessUser, Title = "First" };
        var s2 = new ChatSession { Mode = AssistantMode.Developer, Title = "Second" };
        await _sut.SaveAsync(s1);
        await _sut.SaveAsync(s2);

        var all = await _sut.GetAllAsync();

        all.Should().HaveCount(2);
        all.Select(s => s.Title).Should().Contain("First").And.Contain("Second");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNothingSaved()
    {
        var all = await _sut.GetAllAsync();

        all.Should().BeEmpty();
    }

    // ── Idempotent save (update) ──────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_CalledTwice_UpdatesTitle()
    {
        var session = new ChatSession { Mode = AssistantMode.BusinessUser };
        await _sut.SaveAsync(session);

        session.Title = "Updated title";
        await _sut.SaveAsync(session);

        var loaded = await _sut.GetByIdAsync(session.Id);
        loaded!.Title.Should().Be("Updated title");
    }

    // ── Cascade delete ────────────────────────────────────────────────────

    [Fact]
    public async Task Messages_ArePersistedWithSession()
    {
        var session = new ChatSession { Mode = AssistantMode.BusinessUser };
        session.Messages.Add(new ChatMessage { SessionId = session.Id, Role = MessageRole.User, Content = "First question" });

        await _sut.SaveAsync(session);

        var loaded = await _sut.GetByIdAsync(session.Id);
        loaded!.Messages.Should().HaveCount(1);
        loaded.Messages[0].Content.Should().Be("First question");
        loaded.Messages[0].Role.Should().Be(MessageRole.User);
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }
}
