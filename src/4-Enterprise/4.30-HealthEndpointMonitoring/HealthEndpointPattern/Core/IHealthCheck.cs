namespace HealthEndpointPattern.Core;

public interface IHealthCheck
{
    string Name { get; }
    IReadOnlyList<string> Tags { get; }
    Task<HealthCheckResult> CheckAsync(CancellationToken ct = default);
}
