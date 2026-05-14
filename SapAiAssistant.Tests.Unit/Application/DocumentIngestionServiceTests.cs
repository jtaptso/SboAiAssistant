using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SapAiAssistant.Application.Interfaces;
using SapAiAssistant.Application.Services;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Tests.Unit.Application;

public sealed class DocumentIngestionServiceTests
{
    private readonly IEmbeddingClient _embeddingClient = Substitute.For<IEmbeddingClient>();
    private readonly IVectorStore     _vectorStore     = Substitute.For<IVectorStore>();

    private DocumentIngestionService BuildSut(int chunkSize = 100, int overlap = 10)
    {
        var settings = Options.Create(new RagSettings
        {
            ChunkSize = chunkSize,
            ChunkOverlap = overlap,
            TopK = 3,
            MinSimilarityScore = 0.65f
        });
        _embeddingClient.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 0.1f, 0.2f, 0.3f });

        // Plain-text extractor stub that reads UTF-8 from the stream
        var textExtractor = Substitute.For<IDocumentTextExtractor>();
        textExtractor.CanHandle(Arg.Any<string>()).Returns(true);
        textExtractor.ExtractTextAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(ci => new StreamReader(ci.Arg<Stream>(), Encoding.UTF8).ReadToEndAsync());

        return new DocumentIngestionService(_embeddingClient, _vectorStore, settings, [textExtractor]);
    }

    [Fact]
    public async Task IngestAsync_SingleChunk_CreatesOneChunk()
    {
        var sut = BuildSut(chunkSize: 500);
        var content = "Short document content.";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await sut.IngestAsync("doc.txt", stream);

        await _embeddingClient.Received(1).EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _vectorStore.Received(1).UpsertAsync(Arg.Any<DocumentChunk>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_LongText_CreatesMultipleChunks()
    {
        var sut = BuildSut(chunkSize: 50, overlap: 10);
        // 200 chars → multiple chunks
        var content = new string('x', 200);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await sut.IngestAsync("long.txt", stream);

        // More than one embed call means more than one chunk
        var callCount = _embeddingClient.ReceivedCalls().Count();
        callCount.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task IngestAsync_ChunksHaveCorrectOverlap()
    {
        var sut = BuildSut(chunkSize: 10, overlap: 3);
        // 25 chars: chunks start at 0, 7, 14, 21
        var content = "ABCDEFGHIJKLMNOPQRSTUVWXY";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await sut.IngestAsync("overlap.txt", stream);

        // Expected 4 chunks: 0..10, 7..17, 14..24, 21..25
        await _vectorStore.Received(4).UpsertAsync(Arg.Any<DocumentChunk>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_PersistsDocumentWithCorrectChunkCount()
    {
        var sut = BuildSut(chunkSize: 50, overlap: 0);
        var content = new string('y', 100); // 2 chunks

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await sut.IngestAsync("doc2.txt", stream);

        await _vectorStore.Received(1).UpsertDocumentAsync(
            Arg.Is<Document>(d => d.ChunkCount == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToVectorStore()
    {
        var sut = BuildSut();
        var id = Guid.NewGuid();

        await sut.DeleteAsync(id);

        await _vectorStore.Received(1).DeleteByDocumentAsync(id, Arg.Any<CancellationToken>());
    }
}
