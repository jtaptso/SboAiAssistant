using SapAiAssistant.Domain.ValueObjects;

namespace SapAiAssistant.Application.Interfaces;

public interface ISapContextBuilder
{
    /// <summary>
    /// Fetches SAP data for the given intent and formats it as a plain-text context
    /// string ready to be injected into the LLM prompt.
    /// Returns null when no SAP lookup is required or SAP data is unavailable.
    /// </summary>
    Task<string?> BuildAsync(SapIntent intent, CancellationToken cancellationToken = default);
}
