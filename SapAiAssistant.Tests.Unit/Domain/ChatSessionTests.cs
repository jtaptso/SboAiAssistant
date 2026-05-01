using SapAiAssistant.Domain.Entities;
using FluentAssertions;

namespace SapAiAssistant.Tests.Unit.Domain;

public sealed class ChatSessionTests
{
    [Fact]
    public void Create_SetsIdAndDefaults()
    {
        var session = ChatSession.Create(AssistantMode.BusinessUser);

        session.Id.Should().NotBeEmpty();
        session.Mode.Should().Be(AssistantMode.BusinessUser);
        session.Messages.Should().BeEmpty();
        session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_UsesSuppliedTitle()
    {
        var session = ChatSession.Create(AssistantMode.Developer, "My session");

        session.Title.Should().Be("My session");
    }

    [Fact]
    public void AddMessage_AppendsMessageAndUpdatesTimestamp()
    {
        var session = ChatSession.Create(AssistantMode.BusinessUser);
        var before  = session.UpdatedAt;

        var msg = ChatMessage.Create(session.Id, MessageRole.User, "Hello");
        session.AddMessage(msg);

        session.Messages.Should().HaveCount(1);
        session.Messages[0].Content.Should().Be("Hello");
        session.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void UpdateTitle_ChangesTitle()
    {
        var session = ChatSession.Create(AssistantMode.BusinessUser);
        session.UpdateTitle("Renamed");

        session.Title.Should().Be("Renamed");
    }
}
