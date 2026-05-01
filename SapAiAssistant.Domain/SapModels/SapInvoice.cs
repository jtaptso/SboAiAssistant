namespace SapAiAssistant.Domain.SapModels;

public sealed record SapInvoice(
    int DocEntry,
    int DocNum,
    string CardCode,
    string CardName,
    DateTime DocDate,
    DateTime DocDueDate,
    string DocumentStatus,
    decimal DocTotal,
    decimal PaidToDate,
    string Currency
);
