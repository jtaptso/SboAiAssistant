namespace SapAiAssistant.Application.Interfaces;

/// <summary>Extracts plain text from an uploaded document stream.</summary>
public interface IDocumentTextExtractor
{
    /// <summary>
    /// Returns <c>true</c> when this extractor can handle the given file name
    /// (based on its extension).
    /// </summary>
    bool CanHandle(string fileName);

    /// <summary>Reads <paramref name="stream"/> and returns its full text content.</summary>
    Task<string> ExtractTextAsync(Stream stream, CancellationToken ct = default);
}
