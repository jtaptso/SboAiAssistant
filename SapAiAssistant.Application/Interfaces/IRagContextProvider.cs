namespace SapAiAssistant.Application.Interfaces;

public interface IRagContextProvider
{
    /// <summary>
    /// Retrieves relevant knowledge-base context for the given query.
    /// Returns <c>null</c> when no chunks exceed the similarity threshold.
    /// </summary>
    Task<string?> GetContextAsync(string query, CancellationToken ct = default);
}
