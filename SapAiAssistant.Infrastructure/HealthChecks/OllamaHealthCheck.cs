using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SapAiAssistant.Infrastructure.Configuration;

namespace SapAiAssistant.Infrastructure.HealthChecks;

/// <summary>
/// Verifies that the Ollama service is reachable by calling its root endpoint.
/// Marked <see cref="HealthStatus.Unhealthy"/> when unreachable because the API
/// cannot function without an LLM provider.
/// </summary>
public sealed class OllamaHealthCheck : IHealthCheck
{
    private readonly OllamaOptions _options;

    public OllamaHealthCheck(IOptions<OllamaOptions> options)
        => _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync(_options.BaseUrl, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"Ollama reachable at {_options.BaseUrl}")
                : HealthCheckResult.Degraded(
                    $"Ollama at {_options.BaseUrl} returned HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Ollama at {_options.BaseUrl} is unreachable", ex);
        }
    }
}
