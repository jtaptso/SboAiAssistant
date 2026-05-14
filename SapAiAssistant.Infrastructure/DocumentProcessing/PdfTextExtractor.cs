using System.Text;
using SapAiAssistant.Application.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace SapAiAssistant.Infrastructure.DocumentProcessing;

/// <summary>Extracts text from PDF files using PdfPig.</summary>
public sealed class PdfTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string fileName) =>
        Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractTextAsync(Stream stream, CancellationToken ct = default)
    {
        // PdfPig operates synchronously; wrap to honour the async contract.
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();

        using var pdf = PdfDocument.Open(bytes);

        var sb = new StringBuilder();
        foreach (Page page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return Task.FromResult(sb.ToString());
    }
}
