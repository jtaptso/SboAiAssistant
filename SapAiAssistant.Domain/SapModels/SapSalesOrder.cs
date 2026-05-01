namespace SapAiAssistant.Domain.SapModels;

public sealed record SapSalesOrder(
    int DocEntry,
    int DocNum,
    string CardCode,
    string CardName,
    DateTime DocDate,
    DateTime DocDueDate,
    string DocumentStatus,
    decimal DocTotal,
    string Currency
);
