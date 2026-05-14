using System.Text;
using SapAiAssistant.Application.Interfaces;

namespace SapAiAssistant.Infrastructure.DocumentProcessing;

/// <summary>Extracts text from plain-text (<c>.txt</c>) files.</summary>
public sealed class PlainTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string fileName) =>
        Path.GetExtension(fileName).Equals(".txt", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExtractTextAsync(Stream stream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync(ct);
    }
}
