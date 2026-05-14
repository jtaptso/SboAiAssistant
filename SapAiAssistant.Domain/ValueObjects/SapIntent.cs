namespace SapAiAssistant.Domain.ValueObjects;

/// <summary>
/// Represents the detected intent of a user message, mapping it to an approved
/// SAP B1 operation or a general/developer assistant flow.
/// </summary>
public enum SapIntentKind
{
    /// <summary>No specific SAP intent detected — general conversation.</summary>
    General,

    /// <summary>User is asking about a business partner (customer, supplier, or lead).</summary>
    BusinessPartnerLookup,

    /// <summary>User is asking about an item or product.</summary>
    ItemLookup,

    /// <summary>User is asking about a sales order status or details.</summary>
    SalesOrderLookup,

    /// <summary>User is asking about an invoice or payment status.</summary>
    InvoiceLookup,

    /// <summary>User is asking about general company or system metadata.</summary>
    CompanyMetadata,

    /// <summary>Developer is requesting SAP B1 C# code generation.</summary>
    DeveloperCodeGeneration
}

/// <summary>
/// Value object pairing a detected intent with optional extracted parameters
/// (e.g., CardCode, DocEntry) parsed from the user message.
/// </summary>
public sealed class SapIntent
{
    public SapIntentKind Kind { get; init; }
    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();

    /// <summary>Returns true if this intent requires a SAP data lookup.</summary>
    public bool RequiresSapLookup => Kind is
        SapIntentKind.BusinessPartnerLookup or
        SapIntentKind.ItemLookup or
        SapIntentKind.SalesOrderLookup or
        SapIntentKind.InvoiceLookup or
        SapIntentKind.CompanyMetadata;
}
