namespace SapAiAssistant.Domain.SapModels;

public sealed record SapBusinessPartner(
    string CardCode,
    string CardName,
    SapCardType CardType,
    string? Phone,
    string? Email,
    string? Currency,
    decimal Balance
);
