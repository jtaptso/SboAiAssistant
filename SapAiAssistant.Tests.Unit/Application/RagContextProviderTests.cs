using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SapAiAssistant.Application.Services;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Tests.Unit.Application;

public sealed class RagContextProviderTests
{
    private readonly IEmbeddingClient _embeddingClient = Substitute.For<IEmbeddingClient>();
    private readonly IVectorStore     _vectorStore     = Substitute.For<IVectorStore>();

    private RagContextProvider BuildSut(int topK = 3, float minScore = 0.65f)
    {
        var settings = Options.Create(new RagSettings
        {
            TopK = topK,
            MinSimilarityScore = minScore,
            ChunkSize = 2000,
            ChunkOverlap = 200
        });
        _embeddingClient.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new float[] { 1f, 0f });
        return new RagContextProvider(_embeddingClient, _vectorStore, settings);
    }

    [Fact]
    public async Task GetContextAsync_WhenNoChunksReturned_ReturnsNull()
    {
        _vectorStore.SearchAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DocumentChunk>());

        var sut = BuildSut();
        var result = await sut.GetContextAsync("some query");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetContextAsync_WhenChunksReturned_IncludesKnowledgeBaseHeader()
    {
        var chunk = DocumentChunk.Create(Guid.NewGuid(), "manual.txt", 0, "Relevant content here.", new float[] { 1f, 0f });
        _vectorStore.SearchAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns(new[] { chunk });

        var sut = BuildSut();
        var result = await sut.GetContextAsync("some query");

        result.Should().NotBeNull();
        result!.Should().Contain("## Knowledge Base");
        result.Should().Contain("manual.txt");
        result.Should().Contain("Relevant content here.");
    }

    [Fact]
    public async Task GetContextAsync_PassesTopKAndMinScoreToVectorStore()
    {
        _vectorStore.SearchAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DocumentChunk>());

        var sut = BuildSut(topK: 5, minScore: 0.80f);
        await sut.GetContextAsync("query");

        await _vectorStore.Received(1).SearchAsync(
            Arg.Any<float[]>(), 5, 0.80f, Arg.Any<CancellationToken>());
    }
}
