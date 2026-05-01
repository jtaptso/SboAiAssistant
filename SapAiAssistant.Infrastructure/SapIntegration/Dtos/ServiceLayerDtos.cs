using System.Text.Json.Serialization;

namespace SapAiAssistant.Infrastructure.SapIntegration.Dtos;

// ── Business Partners (/BusinessPartners('<CardCode>')) ───────────────────

internal sealed class SlBusinessPartner
{
    [JsonPropertyName("CardCode")]   public string CardCode    { get; init; } = string.Empty;
    [JsonPropertyName("CardName")]   public string CardName    { get; init; } = string.Empty;
    [JsonPropertyName("CardType")]   public string CardType    { get; init; } = string.Empty;
    [JsonPropertyName("Phone1")]     public string? Phone1     { get; init; }
    [JsonPropertyName("EmailAddress")] public string? Email    { get; init; }
    [JsonPropertyName("Currency")]   public string? Currency   { get; init; }
    [JsonPropertyName("CurrentAccountBalance")] public decimal Balance { get; init; }
}

// ── Items (/Items('<ItemCode>')) ─────────────────────────────────────────

internal sealed class SlItem
{
    [JsonPropertyName("ItemCode")]          public string ItemCode         { get; init; } = string.Empty;
    [JsonPropertyName("ItemName")]          public string ItemName         { get; init; } = string.Empty;
    [JsonPropertyName("ItemType")]          public string ItemType         { get; init; } = string.Empty;
    [JsonPropertyName("QuantityOnStock")]   public decimal QuantityOnStock  { get; init; }
    [JsonPropertyName("InventoryUOM")]      public string? UnitOfMeasure   { get; init; }
    [JsonPropertyName("ItemPrices")]        public IReadOnlyList<SlItemPrice>? Prices { get; init; }
}

internal sealed class SlItemPrice
{
    [JsonPropertyName("PriceList")] public int PriceList { get; init; }
    [JsonPropertyName("Price")]     public decimal Price  { get; init; }
}

// ── Sales Orders (/Orders(<DocEntry>)) ──────────────────────────────────

internal sealed class SlSalesOrder
{
    [JsonPropertyName("DocEntry")]       public int DocEntry         { get; init; }
    [JsonPropertyName("DocNum")]         public int DocNum           { get; init; }
    [JsonPropertyName("CardCode")]       public string CardCode      { get; init; } = string.Empty;
    [JsonPropertyName("CardName")]       public string CardName      { get; init; } = string.Empty;
    [JsonPropertyName("DocDate")]        public string DocDate       { get; init; } = string.Empty;
    [JsonPropertyName("DocDueDate")]     public string DocDueDate    { get; init; } = string.Empty;
    [JsonPropertyName("DocumentStatus")] public string DocumentStatus { get; init; } = string.Empty;
    [JsonPropertyName("DocTotal")]       public decimal DocTotal     { get; init; }
    [JsonPropertyName("DocCurrency")]    public string? Currency     { get; init; }
}

// ── AR Invoices (/Invoices(<DocEntry>)) ─────────────────────────────────

internal sealed class SlInvoice
{
    [JsonPropertyName("DocEntry")]       public int DocEntry          { get; init; }
    [JsonPropertyName("DocNum")]         public int DocNum            { get; init; }
    [JsonPropertyName("CardCode")]       public string CardCode       { get; init; } = string.Empty;
    [JsonPropertyName("CardName")]       public string CardName       { get; init; } = string.Empty;
    [JsonPropertyName("DocDate")]        public string DocDate        { get; init; } = string.Empty;
    [JsonPropertyName("DocDueDate")]     public string DocDueDate     { get; init; } = string.Empty;
    [JsonPropertyName("DocumentStatus")] public string DocumentStatus { get; init; } = string.Empty;
    [JsonPropertyName("DocTotal")]       public decimal DocTotal      { get; init; }
    [JsonPropertyName("PaidToDate")]     public decimal PaidToDate    { get; init; }
    [JsonPropertyName("DocCurrency")]    public string? Currency      { get; init; }
}

// ── Login (/Login) ───────────────────────────────────────────────────────

internal sealed class SlLoginRequest
{
    [JsonPropertyName("CompanyDB")] public string CompanyDb { get; init; } = string.Empty;
    [JsonPropertyName("UserName")]  public string UserName  { get; init; } = string.Empty;
    [JsonPropertyName("Password")]  public string Password  { get; init; } = string.Empty;
}
