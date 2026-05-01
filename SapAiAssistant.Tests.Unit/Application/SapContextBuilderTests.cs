using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SapAiAssistant.Application.Services;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Domain.SapModels;
using SapAiAssistant.Domain.ValueObjects;
using FluentAssertions;

namespace SapAiAssistant.Tests.Unit.Application;

public sealed class SapContextBuilderTests
{
    private readonly ISapAssistantGateway _gateway = Substitute.For<ISapAssistantGateway>();
    private readonly SapContextBuilder _sut;

    public SapContextBuilderTests()
        => _sut = new SapContextBuilder(_gateway, NullLogger<SapContextBuilder>.Instance);

    // ── Business Partner ──────────────────────────────────────────────────

    [Fact]
    public async Task BusinessPartner_Found_ReturnsFormattedBlock()
    {
        var bp = new SapBusinessPartner("C001", "ACME Ltd", SapCardType.Customer,
            "+1 555 1234", "acme@example.com", "USD", 5000m);
        _gateway.GetBusinessPartnerAsync("C001", Arg.Any<CancellationToken>()).Returns(bp);

        var intent = SapIntent.Create(SapIntentKind.BusinessPartnerLookup,
            new Dictionary<string, string> { ["CardCode"] = "C001" });

        var result = await _sut.BuildAsync(intent);

        result.Should().Contain("C001")
              .And.Contain("ACME Ltd")
              .And.Contain("Customer")
              .And.Contain("5000");
    }

    [Fact]
    public async Task BusinessPartner_NotFound_ReturnsNotFoundMessage()
    {
        _gateway.GetBusinessPartnerAsync("X999", Arg.Any<CancellationToken>())
            .Returns((SapBusinessPartner?)null);

        var intent = SapIntent.Create(SapIntentKind.BusinessPartnerLookup,
            new Dictionary<string, string> { ["CardCode"] = "X999" });

        var result = await _sut.BuildAsync(intent);

        result.Should().Contain("X999").And.Contain("No business partner found");
    }

    [Fact]
    public async Task BusinessPartner_MissingCardCodeParam_ReturnsNull()
    {
        var intent = SapIntent.Create(SapIntentKind.BusinessPartnerLookup);

        var result = await _sut.BuildAsync(intent);

        result.Should().BeNull();
        await _gateway.DidNotReceive().GetBusinessPartnerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Item ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Item_Found_ReturnsFormattedBlock()
    {
        var item = new SapItem("IT-001", "Laptop Pro", "itItems", 42m, "EA", 999.99m);
        _gateway.GetItemAsync("IT-001", Arg.Any<CancellationToken>()).Returns(item);

        var intent = SapIntent.Create(SapIntentKind.ItemLookup,
            new Dictionary<string, string> { ["ItemCode"] = "IT-001" });

        var result = await _sut.BuildAsync(intent);

        result.Should().Contain("IT-001").And.Contain("Laptop Pro").And.Contain("42");
    }

    // ── Sales Order ───────────────────────────────────────────────────────

    [Fact]
    public async Task SalesOrder_Found_ReturnsFormattedBlock()
    {
        var order = new SapSalesOrder(100, 1001, "C001", "ACME",
            new DateTime(2026, 1, 10), new DateTime(2026, 1, 20),
            "Open", 2500m, "USD");
        _gateway.GetSalesOrderAsync(100, Arg.Any<CancellationToken>()).Returns(order);

        var intent = SapIntent.Create(SapIntentKind.SalesOrderLookup,
            new Dictionary<string, string> { ["DocEntry"] = "100" });

        var result = await _sut.BuildAsync(intent);

        result.Should().Contain("100").And.Contain("ACME").And.Contain("2500");
    }

    // ── General intent — no SAP call ──────────────────────────────────────

    [Fact]
    public async Task General_Intent_ReturnsNull()
    {
        var result = await _sut.BuildAsync(SapIntent.General());

        result.Should().BeNull();
        await _gateway.DidNotReceive().GetBusinessPartnerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Gateway exception — returns null gracefully ───────────────────────

    [Fact]
    public async Task Gateway_Throws_ReturnsNullWithoutPropagating()
    {
        _gateway.GetBusinessPartnerAsync("ERR", Arg.Any<CancellationToken>())
            .Returns<SapBusinessPartner?>(_ => throw new HttpRequestException("timeout"));

        var intent = SapIntent.Create(SapIntentKind.BusinessPartnerLookup,
            new Dictionary<string, string> { ["CardCode"] = "ERR" });

        var result = await _sut.BuildAsync(intent);

        result.Should().BeNull();
    }
}
