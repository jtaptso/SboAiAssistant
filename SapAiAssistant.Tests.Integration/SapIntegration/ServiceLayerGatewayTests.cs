using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SapAiAssistant.Infrastructure.Configuration;
using SapAiAssistant.Infrastructure.SapIntegration;
using FluentAssertions;

namespace SapAiAssistant.Tests.Integration.SapIntegration;

/// <summary>
/// Contract tests for <see cref="ServiceLayerGateway"/> using a <see cref="MockHttpMessageHandler"/>
/// to simulate Service Layer responses without a live SAP system.
/// </summary>
public sealed class ServiceLayerGatewayTests
{
    private static readonly SapOptions DefaultOptions = new()
    {
        ServiceLayerBaseUrl = "https://sap-test:50000/b1s/v1",
        CompanyDb = "TESTDB",
        UserName  = "manager",
        Password  = "secret",
        TimeoutSeconds = 10
    };

    private ServiceLayerGateway BuildGateway(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(DefaultOptions.ServiceLayerBaseUrl.TrimEnd('/') + "/")
        };
        var options = Options.Create(DefaultOptions);
        var session = new ServiceLayerSession(httpClient, options, NullLogger<ServiceLayerSession>.Instance);

        return new ServiceLayerGateway(
            session,
            options,
            NullLogger<ServiceLayerGateway>.Instance);
    }

    // ── Business Partner ──────────────────────────────────────────────────

    [Fact]
    public async Task GetBusinessPartner_Maps_ResponseCorrectly()
    {
        var json = JsonSerializer.Serialize(new
        {
            CardCode = "C001",
            CardName = "ACME Ltd",
            CardType = "cCustomer",
            Phone1   = "+1 555 1234",
            EmailAddress = "acme@example.com",
            Currency = "USD",
            CurrentAccountBalance = 1500.0
        });

        var handler = new MockHttpMessageHandler(
            loginResponse: OkResponse("{}"),
            dataResponse:  OkResponse(json));

        var gateway = BuildGateway(handler);
        var result  = await gateway.GetBusinessPartnerAsync("C001");

        result.Should().NotBeNull();
        result!.CardCode.Should().Be("C001");
        result.CardName.Should().Be("ACME Ltd");
        result.Balance.Should().Be(1500m);
    }

    [Fact]
    public async Task GetBusinessPartner_Returns_Null_On404()
    {
        var handler = new MockHttpMessageHandler(
            loginResponse: OkResponse("{}"),
            dataResponse:  new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await BuildGateway(handler).GetBusinessPartnerAsync("MISSING");

        result.Should().BeNull();
    }

    // ── Item ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetItem_Maps_ResponseCorrectly()
    {
        var json = JsonSerializer.Serialize(new
        {
            ItemCode        = "IT-001",
            ItemName        = "Laptop Pro",
            ItemType        = "itItems",
            QuantityOnStock = 42.0,
            InventoryUOM    = "EA",
            ItemPrices      = new[] { new { PriceList = 1, Price = 999.99 } }
        });

        var handler = new MockHttpMessageHandler(OkResponse("{}"), OkResponse(json));
        var result  = await BuildGateway(handler).GetItemAsync("IT-001");

        result.Should().NotBeNull();
        result!.ItemCode.Should().Be("IT-001");
        result.QuantityOnStock.Should().Be(42m);
        result.Price.Should().Be(999.99m);
    }

    // ── Sales Order ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSalesOrder_Maps_StatusAndDates()
    {
        var json = JsonSerializer.Serialize(new
        {
            DocEntry       = 100,
            DocNum         = 1001,
            CardCode       = "C001",
            CardName       = "ACME",
            DocDate        = "2026-01-10",
            DocDueDate     = "2026-01-20",
            DocumentStatus = "bost_Open",
            DocTotal       = 2500.0,
            DocCurrency    = "USD"
        });

        var handler = new MockHttpMessageHandler(OkResponse("{}"), OkResponse(json));
        var result  = await BuildGateway(handler).GetSalesOrderAsync(100);

        result.Should().NotBeNull();
        result!.DocumentStatus.Should().Be("Open");
        result.DocDate.Should().Be(new DateTime(2026, 1, 10));
        result.DocTotal.Should().Be(2500m);
    }

    // ── Availability ──────────────────────────────────────────────────────

    [Fact]
    public async Task IsAvailable_ReturnsTrue_WhenLoginSucceeds()
    {
        var handler = new MockHttpMessageHandler(OkResponse("{}"), OkResponse("{}"));
        var result  = await BuildGateway(handler).IsAvailableAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailable_ReturnsFalse_WhenLoginFails()
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.Unauthorized),
            new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var result = await BuildGateway(handler).IsAvailableAsync();

        result.Should().BeFalse();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static HttpResponseMessage OkResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
}

/// <summary>
/// Minimal fake HTTP handler: returns <see cref="LoginResponse"/> for the first request
/// (the /Login call) and <see cref="DataResponse"/> for every subsequent call.
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _loginResponse;
    private readonly HttpResponseMessage _dataResponse;
    private int _callCount;

    public MockHttpMessageHandler(HttpResponseMessage loginResponse, HttpResponseMessage dataResponse)
    {
        _loginResponse = loginResponse;
        _dataResponse  = dataResponse;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = Interlocked.Increment(ref _callCount) == 1
            ? _loginResponse
            : _dataResponse;

        return Task.FromResult(response);
    }
}
