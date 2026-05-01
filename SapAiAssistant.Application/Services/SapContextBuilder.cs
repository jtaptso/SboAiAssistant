using SapAiAssistant.Application.Interfaces;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace SapAiAssistant.Application.Services;

/// <summary>
/// Dispatches to <see cref="ISapAssistantGateway"/> based on detected intent and
/// formats the retrieved SAP data into a plain-text block for LLM prompt injection.
/// </summary>
public sealed class SapContextBuilder : ISapContextBuilder
{
    private readonly ISapAssistantGateway _sap;
    private readonly ILogger<SapContextBuilder> _logger;

    public SapContextBuilder(ISapAssistantGateway sap, ILogger<SapContextBuilder> logger)
    {
        _sap = sap;
        _logger = logger;
    }

    public async Task<string?> BuildAsync(SapIntent intent, CancellationToken cancellationToken = default)
    {
        try
        {
            return intent.Kind switch
            {
                SapIntentKind.BusinessPartnerLookup => await BuildBusinessPartnerContextAsync(intent, cancellationToken),
                SapIntentKind.ItemLookup            => await BuildItemContextAsync(intent, cancellationToken),
                SapIntentKind.SalesOrderLookup      => await BuildSalesOrderContextAsync(intent, cancellationToken),
                SapIntentKind.InvoiceLookup         => await BuildInvoiceContextAsync(intent, cancellationToken),
                SapIntentKind.CompanyMetadata        => "[SAP] Company metadata lookup is not yet implemented.",
                _                                   => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SAP context fetch failed for intent {Kind}", intent.Kind);
            return null;
        }
    }

    private async Task<string?> BuildBusinessPartnerContextAsync(SapIntent intent, CancellationToken ct)
    {
        if (!intent.TryGetParameter("CardCode", out var cardCode))
            return null;

        var bp = await _sap.GetBusinessPartnerAsync(cardCode, ct);
        if (bp is null)
            return $"[SAP] No business partner found for CardCode '{cardCode}'.";

        return $"""
            [SAP Business Partner]
            CardCode : {bp.CardCode}
            Name     : {bp.CardName}
            Type     : {bp.CardType}
            Phone    : {bp.Phone ?? "—"}
            Email    : {bp.Email ?? "—"}
            Currency : {bp.Currency ?? "—"}
            Balance  : {bp.Balance}
            """;
    }

    private async Task<string?> BuildItemContextAsync(SapIntent intent, CancellationToken ct)
    {
        if (!intent.TryGetParameter("ItemCode", out var itemCode))
            return null;

        var item = await _sap.GetItemAsync(itemCode, ct);
        if (item is null)
            return $"[SAP] No item found for ItemCode '{itemCode}'.";

        return $"""
            [SAP Item]
            ItemCode : {item.ItemCode}
            Name     : {item.ItemName}
            Type     : {item.ItemType}
            Stock    : {item.QuantityOnStock} {item.UnitOfMeasure}
            Price    : {item.Price}
            """;
    }

    private async Task<string?> BuildSalesOrderContextAsync(SapIntent intent, CancellationToken ct)
    {
        if (!intent.TryGetParameter("DocEntry", out var docEntryStr) || !int.TryParse(docEntryStr, out var docEntry))
            return null;

        var order = await _sap.GetSalesOrderAsync(docEntry, ct);
        if (order is null)
            return $"[SAP] No sales order found for DocEntry {docEntry}.";

        return $"""
            [SAP Sales Order]
            DocEntry : {order.DocEntry}
            DocNum   : {order.DocNum}
            Customer : {order.CardCode} ({order.CardName})
            Date     : {order.DocDate:d}
            Due      : {order.DocDueDate:d}
            Status   : {order.DocumentStatus}
            Total    : {order.DocTotal} {order.Currency}
            """;
    }

    private async Task<string?> BuildInvoiceContextAsync(SapIntent intent, CancellationToken ct)
    {
        if (!intent.TryGetParameter("DocEntry", out var docEntryStr) || !int.TryParse(docEntryStr, out var docEntry))
            return null;

        var invoice = await _sap.GetInvoiceAsync(docEntry, ct);
        if (invoice is null)
            return $"[SAP] No invoice found for DocEntry {docEntry}.";

        return $"""
            [SAP Invoice]
            DocEntry : {invoice.DocEntry}
            DocNum   : {invoice.DocNum}
            Customer : {invoice.CardCode} ({invoice.CardName})
            Date     : {invoice.DocDate:d}
            Due      : {invoice.DocDueDate:d}
            Status   : {invoice.DocumentStatus}
            Total    : {invoice.DocTotal} {invoice.Currency}
            Paid     : {invoice.PaidToDate} {invoice.Currency}
            """;
    }
}
