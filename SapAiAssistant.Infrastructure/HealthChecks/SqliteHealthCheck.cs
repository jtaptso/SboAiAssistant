using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SapAiAssistant.Infrastructure.Persistence;

namespace SapAiAssistant.Infrastructure.HealthChecks;

/// <summary>
/// Verifies that the SQLite database file exists and EF Core can open a connection.
/// </summary>
public sealed class SqliteHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SqliteHealthCheck(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("SQLite database is reachable")
                : HealthCheckResult.Unhealthy("SQLite database cannot be reached");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQLite health check threw an exception", ex);
        }
    }
}
