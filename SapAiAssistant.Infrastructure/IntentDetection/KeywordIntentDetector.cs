using System.Text.RegularExpressions;
using SapAiAssistant.Application.Interfaces;
using SapAiAssistant.Domain.Entities;
using SapAiAssistant.Domain.ValueObjects;

namespace SapAiAssistant.Infrastructure.IntentDetection;

/// <summary>
/// Keyword-based intent detector for v1. Identifies the SAP operation being requested
/// using simple pattern matching. A future version may delegate classification to the LLM.
/// </summary>
public sealed class KeywordIntentDetector : IIntentDetector
{
    // Matches typical SAP item/business-partner codes: one or more letters followed by digits (e.g. C001, IT-001)
    private static readonly Regex CodePattern =
        new(@"\b([A-Z][A-Z0-9\-]{1,19})\b", RegexOptions.Compiled);

    // Matches numeric document entry / document number references
    private static readonly Regex DocEntryPattern =
        new(@"\b(\d{1,9})\b", RegexOptions.Compiled);

    public Task<SapIntent> DetectAsync(
        string userMessage,
        AssistantMode mode,
        CancellationToken cancellationToken = default)
    {
        // Developer mode always maps to code-generation intent
        if (mode == AssistantMode.Developer)
            return Task.FromResult(new SapIntent { Kind = SapIntentKind.DeveloperCodeGeneration });

        var lower = userMessage.ToLowerInvariant();

        // Business Partner
        if (ContainsAny(lower, "business partner", "customer", "vendor", "supplier",
                         "card code", "cardcode", " bp ", "client"))
        {
            var parms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var match = CodePattern.Match(userMessage);
            if (match.Success)
                parms["CardCode"] = match.Value.ToUpperInvariant();

            return Task.FromResult(new SapIntent { Kind = SapIntentKind.BusinessPartnerLookup, Parameters = parms });
        }

        // Item / Product
        if (ContainsAny(lower, "item", "product", "article", "stock", "inventory",
                         "item code", "itemcode"))
        {
            var parms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var match = CodePattern.Match(userMessage);
            if (match.Success)
                parms["ItemCode"] = match.Value.ToUpperInvariant();

            return Task.FromResult(new SapIntent { Kind = SapIntentKind.ItemLookup, Parameters = parms });
        }

        // Sales Order
        if (ContainsAny(lower, "sales order", "sales doc", "order number", "order #",
                         "order no", " so "))
        {
            var parms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var match = DocEntryPattern.Match(userMessage);
            if (match.Success)
                parms["DocEntry"] = match.Value;

            return Task.FromResult(new SapIntent { Kind = SapIntentKind.SalesOrderLookup, Parameters = parms });
        }

        // Invoice
        if (ContainsAny(lower, "invoice", "ar invoice", "ap invoice", "billing",
                         "outstanding", "overdue", "amount due"))
        {
            var parms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var match = DocEntryPattern.Match(userMessage);
            if (match.Success)
                parms["DocEntry"] = match.Value;

            return Task.FromResult(new SapIntent { Kind = SapIntentKind.InvoiceLookup, Parameters = parms });
        }

        // Company Metadata
        if (ContainsAny(lower, "company db", "company info", "system info", "tenant"))
            return Task.FromResult(new SapIntent { Kind = SapIntentKind.CompanyMetadata });

        return Task.FromResult(new SapIntent());
    }

    private static bool ContainsAny(string source, params string[] keywords)
        => keywords.Any(source.Contains);
}
