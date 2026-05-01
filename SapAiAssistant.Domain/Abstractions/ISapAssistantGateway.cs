using SapAiAssistant.Domain.SapModels;

namespace SapAiAssistant.Domain.Abstractions;

public interface ISapAssistantGateway
{
    Task<SapBusinessPartner?> GetBusinessPartnerAsync(string cardCode, CancellationToken cancellationToken = default);
    Task<SapItem?> GetItemAsync(string itemCode, CancellationToken cancellationToken = default);
    Task<SapSalesOrder?> GetSalesOrderAsync(int docEntry, CancellationToken cancellationToken = default);
    Task<SapInvoice?> GetInvoiceAsync(int docEntry, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
