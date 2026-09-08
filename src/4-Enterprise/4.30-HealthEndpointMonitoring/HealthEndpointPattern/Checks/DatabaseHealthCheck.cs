using HealthEndpointPattern.Core;

namespace HealthEndpointPattern.Checks;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly Func<bool> _canConnect;

    public DatabaseHealthCheck(Func<bool>? canConnect = null)
        => _canConnect = canConnect ?? (() => true);

    public string Name => "maple-db";
    public IReadOnlyList<string> Tags { get; } = ["readiness"];

    public Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        if (!_canConnect())
            return Task.FromResult(HealthCheckResult.Unhealthy("Cannot reach PostgreSQL cluster"));
        return Task.FromResult(HealthCheckResult.Healthy("PostgreSQL connection pool active"));
    }
}
