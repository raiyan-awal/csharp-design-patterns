using HealthEndpointPattern.Core;

namespace HealthEndpointPattern.Checks;

public sealed class ObjectStorageHealthCheck : IHealthCheck
{
    private readonly Func<bool> _canReach;

    public ObjectStorageHealthCheck(Func<bool>? canReach = null)
        => _canReach = canReach ?? (() => true);

    public string Name => "object-storage";
    public IReadOnlyList<string> Tags { get; } = ["readiness"];

    public Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        if (!_canReach())
            return Task.FromResult(HealthCheckResult.Unhealthy("Maple Object Storage bucket unreachable"));
        return Task.FromResult(HealthCheckResult.Healthy("Maple Object Storage bucket accessible"));
    }
}
