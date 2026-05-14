using SapAiAssistant.Domain.ValueObjects;
using SapAiAssistant.Domain.Entities;
using FluentAssertions;

namespace SapAiAssistant.Tests.Unit.Domain;

public sealed class SapIntentTests
{
    [Fact]
    public void General_ReturnsGeneralKind()
    {
        var intent = new SapIntent();

        intent.Kind.Should().Be(SapIntentKind.General);
        intent.RequiresSapLookup.Should().BeFalse();
    }

    [Theory]
    [InlineData(SapIntentKind.BusinessPartnerLookup)]
    [InlineData(SapIntentKind.ItemLookup)]
    [InlineData(SapIntentKind.SalesOrderLookup)]
    [InlineData(SapIntentKind.InvoiceLookup)]
    [InlineData(SapIntentKind.CompanyMetadata)]
    public void RequiresSapLookup_IsTrueForSapKinds(SapIntentKind kind)
    {
        new SapIntent { Kind = kind }.RequiresSapLookup.Should().BeTrue();
    }

    [Theory]
    [InlineData(SapIntentKind.General)]
    [InlineData(SapIntentKind.DeveloperCodeGeneration)]
    public void RequiresSapLookup_IsFalseForNonSapKinds(SapIntentKind kind)
    {
        new SapIntent { Kind = kind }.RequiresSapLookup.Should().BeFalse();
    }

    [Fact]
    public void Parameters_ReturnsTrueWhenPresent()
    {
        var intent = new SapIntent
        {
            Kind = SapIntentKind.BusinessPartnerLookup,
            Parameters = new Dictionary<string, string> { ["CardCode"] = "C001" }
        };

        intent.Parameters.TryGetValue("CardCode", out var value).Should().BeTrue();
        value.Should().Be("C001");
    }

    [Fact]
    public void Parameters_ReturnsFalseWhenAbsent()
    {
        var intent = new SapIntent();

        intent.Parameters.TryGetValue("CardCode", out _).Should().BeFalse();
    }

    [Fact]
    public void ConversationContext_ObjectInitializer_SetsProperties()
    {
        var sessionId = Guid.NewGuid();
        var ctx = new ConversationContext { SessionId = sessionId, Mode = AssistantMode.BusinessUser, UserMessage = "Show me customer C001" };

        var enriched = new ConversationContext { SessionId = ctx.SessionId, Mode = ctx.Mode, UserMessage = ctx.UserMessage, History = ctx.History, SapDataContext = "[SAP] CardCode: C001", RagContext = ctx.RagContext };

        ctx.SapDataContext.Should().BeNull();
        enriched.SapDataContext.Should().Be("[SAP] CardCode: C001");
        enriched.UserMessage.Should().Be(ctx.UserMessage);
    }
}
