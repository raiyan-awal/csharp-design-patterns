using HealthEndpointPattern.Core;

namespace HealthEndpointPattern.Checks;

public sealed class ExternalApiHealthCheck : IHealthCheck
{
    private readonly string _apiName;
    private readonly Func<bool> _isReachable;

    public ExternalApiHealthCheck(string apiName = "Canada Post Tracking API",
                                  Func<bool>? isReachable = null)
    {
        _apiName     = apiName;
        _isReachable = isReachable ?? (() => true);
    }

    public string Name => "external-api";
    public IReadOnlyList<string> Tags { get; } = ["readiness"];

    public Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        if (!_isReachable())
            return Task.FromResult(HealthCheckResult.Unhealthy($"{_apiName} is unreachable"));
        return Task.FromResult(HealthCheckResult.Healthy($"{_apiName} responding"));
    }
}
