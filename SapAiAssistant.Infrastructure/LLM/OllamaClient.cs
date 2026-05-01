using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Infrastructure.Configuration;

namespace SapAiAssistant.Infrastructure.LLM;

public sealed class OllamaClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaClient> _logger;

    public OllamaClient(HttpClient http, IOptions<OllamaOptions> options, ILogger<OllamaClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var requestBody = new OllamaRequest(_options.Model, prompt, Stream: false);
        var json = JsonSerializer.Serialize(requestBody, OllamaJsonContext.Default.OllamaRequest);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Calling Ollama model {Model}", _options.Model);

        var response = await _http.PostAsync("/api/generate", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize(responseJson, OllamaJsonContext.Default.OllamaResponse);

        return result?.Response ?? throw new InvalidOperationException("Ollama returned an empty response.");
    }
}

internal sealed record OllamaRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("stream")] bool Stream
);

internal sealed record OllamaResponse(
    [property: JsonPropertyName("response")] string Response
);

[JsonSerializable(typeof(OllamaRequest))]
[JsonSerializable(typeof(OllamaResponse))]
internal sealed partial class OllamaJsonContext : JsonSerializerContext { }
