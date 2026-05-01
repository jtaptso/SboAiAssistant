namespace SapAiAssistant.Domain.SapModels;

public sealed record SapItem(
    string ItemCode,
    string ItemName,
    string ItemType,
    decimal QuantityOnStock,
    string? UnitOfMeasure,
    decimal Price
);
