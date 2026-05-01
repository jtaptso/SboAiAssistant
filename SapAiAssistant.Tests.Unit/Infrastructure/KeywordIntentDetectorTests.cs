using SapAiAssistant.Domain.Entities;
using SapAiAssistant.Domain.ValueObjects;
using SapAiAssistant.Infrastructure.IntentDetection;
using FluentAssertions;

namespace SapAiAssistant.Tests.Unit.Infrastructure;

public sealed class KeywordIntentDetectorTests
{
    private readonly KeywordIntentDetector _sut = new();

    // ── Developer mode ────────────────────────────────────────────────────

    [Fact]
    public async Task Developer_Mode_AlwaysReturnsDeveloperCodeGeneration()
    {
        var intent = await _sut.DetectAsync("What is a business partner?", AssistantMode.Developer);

        intent.Kind.Should().Be(SapIntentKind.DeveloperCodeGeneration);
    }

    // ── Business Partner ──────────────────────────────────────────────────

    [Theory]
    [InlineData("Show me the customer C001")]
    [InlineData("Look up business partner BP-200")]
    [InlineData("Who is supplier VENDOR01?")]
    [InlineData("Tell me about this client")]
    public async Task BusinessPartner_Keywords_ReturnBusinessPartnerLookup(string message)
    {
        var intent = await _sut.DetectAsync(message, AssistantMode.BusinessUser);

        intent.Kind.Should().Be(SapIntentKind.BusinessPartnerLookup);
    }

    [Fact]
    public async Task BusinessPartner_ExtractsCardCode_WhenPresent()
    {
        var intent = await _sut.DetectAsync("Show me customer C001 details", AssistantMode.BusinessUser);

        intent.TryGetParameter("CardCode", out var code).Should().BeTrue();
        code.Should().Be("C001");
    }

    // ── Item ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("What is the stock for item IT-001?")]
    [InlineData("Show me inventory for product A100")]
    [InlineData("What is the article count?")]
    public async Task Item_Keywords_ReturnItemLookup(string message)
    {
        var intent = await _sut.DetectAsync(message, AssistantMode.BusinessUser);

        intent.Kind.Should().Be(SapIntentKind.ItemLookup);
    }

    // ── Sales Order ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("What is the status of sales order 1234?")]
    [InlineData("Show me SO 9999")]
    [InlineData("Check order number 5678")]
    public async Task SalesOrder_Keywords_ReturnSalesOrderLookup(string message)
    {
        var intent = await _sut.DetectAsync(message, AssistantMode.BusinessUser);

        intent.Kind.Should().Be(SapIntentKind.SalesOrderLookup);
    }

    [Fact]
    public async Task SalesOrder_ExtractsDocEntry_WhenPresent()
    {
        var intent = await _sut.DetectAsync("Get me sales order 4242", AssistantMode.BusinessUser);

        intent.TryGetParameter("DocEntry", out var docEntry).Should().BeTrue();
        docEntry.Should().Be("4242");
    }

    // ── Invoice ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Is invoice 100 paid?")]
    [InlineData("Show me the invoice 200")]
    [InlineData("What is the outstanding balance on billing 200?")]
    public async Task Invoice_Keywords_ReturnInvoiceLookup(string message)
    {
        var intent = await _sut.DetectAsync(message, AssistantMode.BusinessUser);

        intent.Kind.Should().Be(SapIntentKind.InvoiceLookup);
    }

    // ── General fallback ──────────────────────────────────────────────────

    [Theory]
    [InlineData("How does SAP Business One handle multi-currency?")]
    [InlineData("What is a journal entry?")]
    public async Task Unrecognised_Message_ReturnsGeneral(string message)
    {
        var intent = await _sut.DetectAsync(message, AssistantMode.BusinessUser);

        intent.Kind.Should().Be(SapIntentKind.General);
    }
}
