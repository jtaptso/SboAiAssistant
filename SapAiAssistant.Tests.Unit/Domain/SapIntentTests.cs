using SapAiAssistant.Domain.ValueObjects;
using SapAiAssistant.Domain.Entities;
using FluentAssertions;

namespace SapAiAssistant.Tests.Unit.Domain;

public sealed class SapIntentTests
{
    [Fact]
    public void General_ReturnsGeneralKind()
    {
        var intent = SapIntent.General();

        intent.Kind.Should().Be(SapIntentKind.General);
        intent.RequiresSapLookup().Should().BeFalse();
    }

    [Theory]
    [InlineData(SapIntentKind.BusinessPartnerLookup)]
    [InlineData(SapIntentKind.ItemLookup)]
    [InlineData(SapIntentKind.SalesOrderLookup)]
    [InlineData(SapIntentKind.InvoiceLookup)]
    [InlineData(SapIntentKind.CompanyMetadata)]
    public void RequiresSapLookup_IsTrueForSapKinds(SapIntentKind kind)
    {
        SapIntent.Create(kind).RequiresSapLookup().Should().BeTrue();
    }

    [Theory]
    [InlineData(SapIntentKind.General)]
    [InlineData(SapIntentKind.DeveloperCodeGeneration)]
    public void RequiresSapLookup_IsFalseForNonSapKinds(SapIntentKind kind)
    {
        SapIntent.Create(kind).RequiresSapLookup().Should().BeFalse();
    }

    [Fact]
    public void TryGetParameter_ReturnsTrueWhenPresent()
    {
        var intent = SapIntent.Create(SapIntentKind.BusinessPartnerLookup,
            new Dictionary<string, string> { ["CardCode"] = "C001" });

        intent.TryGetParameter("CardCode", out var value).Should().BeTrue();
        value.Should().Be("C001");
    }

    [Fact]
    public void TryGetParameter_ReturnsFalseWhenAbsent()
    {
        var intent = SapIntent.General();

        intent.TryGetParameter("CardCode", out _).Should().BeFalse();
    }

    [Fact]
    public void ConversationContext_WithSapContext_ReturnsNewInstanceWithContext()
    {
        var ctx = ConversationContext.Create(
            Guid.NewGuid(), AssistantMode.BusinessUser, "Show me customer C001");

        var enriched = ctx.WithSapContext("[SAP] CardCode: C001");

        ctx.SapDataContext.Should().BeNull();
        enriched.SapDataContext.Should().Be("[SAP] CardCode: C001");
        enriched.UserMessage.Should().Be(ctx.UserMessage);
    }
}
