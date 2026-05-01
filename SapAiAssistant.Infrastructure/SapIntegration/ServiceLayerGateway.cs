using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Domain.SapModels;
using SapAiAssistant.Infrastructure.Configuration;
using SapAiAssistant.Infrastructure.SapIntegration.Dtos;

namespace SapAiAssistant.Infrastructure.SapIntegration;

/// <summary>
/// SAP Business One Service Layer adapter. Covers the read-only v1 allow-list:
/// business partners, items, sales orders, AR invoices, and availability check.
/// </summary>
public sealed class ServiceLayerGateway : ISapAssistantGateway, IAsyncDisposable
{
    private readonly ServiceLayerSession _session;
    private readonly ILogger<ServiceLayerGateway> _logger;
    private readonly string _baseUrl;

    // SAP Service Layer returns dates as "YYYY-MM-DD"
    private static readonly string[] DateFormats = ["yyyy-MM-dd", "yyyyMMdd"];

    public ServiceLayerGateway(
        ServiceLayerSession session,
        IOptions<SapOptions> options,
        ILogger<ServiceLayerGateway> logger)
    {
        _session = session;
        _logger  = logger;
        _baseUrl = options.Value.ServiceLayerBaseUrl.TrimEnd('/');
    }

    // ── Business Partner ─────────────────────────────────────────────────

    public async Task<SapBusinessPartner?> GetBusinessPartnerAsync(
        string cardCode,
        CancellationToken cancellationToken = default)
    {
        var url = $"BusinessPartners('{Uri.EscapeDataString(cardCode)}')";
        var dto = await GetAsync<SlBusinessPartner>(url, cancellationToken);
        return dto is null ? null : Map(dto);
    }

    // ── Item ─────────────────────────────────────────────────────────────

    public async Task<SapItem?> GetItemAsync(
        string itemCode,
        CancellationToken cancellationToken = default)
    {
        var url = $"Items('{Uri.EscapeDataString(itemCode)}')";
        var dto = await GetAsync<SlItem>(url, cancellationToken);
        return dto is null ? null : Map(dto);
    }

    // ── Sales Order ──────────────────────────────────────────────────────

    public async Task<SapSalesOrder?> GetSalesOrderAsync(
        int docEntry,
        CancellationToken cancellationToken = default)
    {
        var dto = await GetAsync<SlSalesOrder>($"Orders({docEntry})", cancellationToken);
        return dto is null ? null : Map(dto);
    }

    // ── AR Invoice ───────────────────────────────────────────────────────

    public async Task<SapInvoice?> GetInvoiceAsync(
        int docEntry,
        CancellationToken cancellationToken = default)
    {
        var dto = await GetAsync<SlInvoice>($"Invoices({docEntry})", cancellationToken);
        return dto is null ? null : Map(dto);
    }

    // ── Availability ─────────────────────────────────────────────────────

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _session.EnsureSessionAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SAP Service Layer availability check failed.");
            return false;
        }
    }

    // ── Internals ────────────────────────────────────────────────────────

    private async Task<T?> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken)
        where T : class
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/{relativeUrl}");
        request.Headers.Add("Prefer", "odata.maxpagesize=1");

        HttpResponseMessage response;
        try
        {
            response = await _session.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAP Service Layer request failed for {Url}", relativeUrl);
            return null;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("SAP Service Layer returned {Status} for {Url}",
                (int)response.StatusCode, relativeUrl);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    // ── Mapping helpers ──────────────────────────────────────────────────

    private static SapBusinessPartner Map(SlBusinessPartner dto) => new(
        dto.CardCode,
        dto.CardName,
        ParseCardType(dto.CardType),
        dto.Phone1.NullIfEmpty(),
        dto.Email.NullIfEmpty(),
        dto.Currency.NullIfEmpty(),
        dto.Balance);

    private static SapItem Map(SlItem dto) => new(
        dto.ItemCode,
        dto.ItemName,
        dto.ItemType,
        dto.QuantityOnStock,
        dto.UnitOfMeasure.NullIfEmpty(),
        dto.Prices?.FirstOrDefault(p => p.PriceList == 1)?.Price ?? 0m);

    private static SapSalesOrder Map(SlSalesOrder dto) => new(
        dto.DocEntry,
        dto.DocNum,
        dto.CardCode,
        dto.CardName,
        ParseDate(dto.DocDate),
        ParseDate(dto.DocDueDate),
        FriendlyStatus(dto.DocumentStatus),
        dto.DocTotal,
        dto.Currency ?? string.Empty);

    private static SapInvoice Map(SlInvoice dto) => new(
        dto.DocEntry,
        dto.DocNum,
        dto.CardCode,
        dto.CardName,
        ParseDate(dto.DocDate),
        ParseDate(dto.DocDueDate),
        FriendlyStatus(dto.DocumentStatus),
        dto.DocTotal,
        dto.PaidToDate,
        dto.Currency ?? string.Empty);

    private static SapCardType ParseCardType(string value) => value switch
    {
        "cCustomer" => SapCardType.Customer,
        "cSupplier"  => SapCardType.Supplier,
        "cLead"      => SapCardType.Lead,
        _            => SapCardType.Customer
    };

    private static string FriendlyStatus(string status) => status switch
    {
        "bost_Open"   => "Open",
        "bost_Close"  => "Closed",
        "bost_Cancel" => "Cancelled",
        _             => status
    };

    private static DateTime ParseDate(string value)
        => DateTime.TryParseExact(value, DateFormats,
               System.Globalization.CultureInfo.InvariantCulture,
               System.Globalization.DateTimeStyles.None, out var dt)
           ? dt
           : DateTime.MinValue;

    public async ValueTask DisposeAsync() => await _session.DisposeAsync();
}

file static class StringExtensions
{
    public static string? NullIfEmpty(this string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
