using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Infrastructure.Configuration;

namespace SapAiAssistant.Infrastructure.HealthChecks;

/// <summary>
/// Verifies that the SAP Business One Service Layer is reachable.
/// Reported as <see cref="HealthStatus.Degraded"/> rather than Unhealthy because the
/// assistant degrades gracefully — chat continues without SAP grounding data.
/// If no Service Layer URL is configured the check is skipped (Healthy).
/// </summary>
public sealed class SapHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SapOptions _options;

    public SapHealthCheck(IServiceScopeFactory scopeFactory, IOptions<SapOptions> options)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ServiceLayerBaseUrl))
            return HealthCheckResult.Healthy("SAP Service Layer is not configured (skipped)");

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var gateway = scope.ServiceProvider.GetRequiredService<ISapAssistantGateway>();

            var available = await gateway.IsAvailableAsync(cancellationToken);
            return available
                ? HealthCheckResult.Healthy("SAP Service Layer is reachable")
                : HealthCheckResult.Degraded("SAP Service Layer login failed — chat continues without SAP data");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded(
                "SAP Service Layer health check threw an exception — chat continues without SAP data", ex);
        }
    }
}
