using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Infrastructure.Configuration;

namespace SapAiAssistant.Infrastructure.LLM;

public sealed class OllamaEmbeddingClient : IEmbeddingClient
{
    private readonly HttpClient _http;
    private readonly EmbeddingOptions _options;
    private readonly ILogger<OllamaEmbeddingClient> _logger;

    public OllamaEmbeddingClient(HttpClient http, IOptions<EmbeddingOptions> options, ILogger<OllamaEmbeddingClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var requestBody = new EmbeddingRequest(_options.Model, text);
        var json = JsonSerializer.Serialize(requestBody, EmbeddingJsonContext.Default.EmbeddingRequest);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Calling Ollama embeddings with model {Model}", _options.Model);

        var response = await _http.PostAsync("/api/embeddings", content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize(responseJson, EmbeddingJsonContext.Default.EmbeddingResponse);

        return result?.Embedding ?? throw new InvalidOperationException("Ollama embedding returned an empty response.");
    }
}

internal sealed record EmbeddingRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("prompt")] string Prompt
);

internal sealed record EmbeddingResponse(
    [property: JsonPropertyName("embedding")] float[] Embedding
);

[JsonSerializable(typeof(EmbeddingRequest))]
[JsonSerializable(typeof(EmbeddingResponse))]
internal sealed partial class EmbeddingJsonContext : JsonSerializerContext { }
