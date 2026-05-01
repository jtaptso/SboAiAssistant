using Microsoft.Extensions.Logging;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Domain.SapModels;

namespace SapAiAssistant.Infrastructure.SapIntegration;

/// <summary>
/// Stub SAP gateway. Replace with real Service Layer implementation in Phase 4.
/// </summary>
public sealed class SapAssistantGatewayStub : ISapAssistantGateway
{
    private readonly ILogger<SapAssistantGatewayStub> _logger;

    public SapAssistantGatewayStub(ILogger<SapAssistantGatewayStub> logger)
    {
        _logger = logger;
    }

    public Task<SapBusinessPartner?> GetBusinessPartnerAsync(string cardCode, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("SAP gateway is a stub. Business partner lookup for '{CardCode}' was not executed.", cardCode);
        return Task.FromResult<SapBusinessPartner?>(null);
    }

    public Task<SapItem?> GetItemAsync(string itemCode, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("SAP gateway is a stub. Item lookup for '{ItemCode}' was not executed.", itemCode);
        return Task.FromResult<SapItem?>(null);
    }

    public Task<SapSalesOrder?> GetSalesOrderAsync(int docEntry, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("SAP gateway is a stub. Sales order lookup for DocEntry {DocEntry} was not executed.", docEntry);
        return Task.FromResult<SapSalesOrder?>(null);
    }

    public Task<SapInvoice?> GetInvoiceAsync(int docEntry, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("SAP gateway is a stub. Invoice lookup for DocEntry {DocEntry} was not executed.", docEntry);
        return Task.FromResult<SapInvoice?>(null);
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false); // stub is always "unavailable"
    }
}
