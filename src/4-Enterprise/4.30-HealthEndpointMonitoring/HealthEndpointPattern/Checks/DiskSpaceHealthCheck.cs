using HealthEndpointPattern.Core;

namespace HealthEndpointPattern.Checks;

public sealed class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly Func<long> _getFreeBytes;
    private const long DegradedThreshold = 10L * 1024 * 1024 * 1024;  // 10 GB
    private const long UnhealthyThreshold =  2L * 1024 * 1024 * 1024;  //  2 GB

    public DiskSpaceHealthCheck(Func<long>? getFreeBytes = null)
        => _getFreeBytes = getFreeBytes ?? (() => 50L * 1024 * 1024 * 1024);

    public string Name => "disk-space";
    public IReadOnlyList<string> Tags { get; } = ["liveness", "readiness"];

    public Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var freeBytes = _getFreeBytes();
        var freeGb    = Math.Round(freeBytes / (1024.0 * 1024 * 1024), 1);
        IReadOnlyDictionary<string, object> data = new Dictionary<string, object> { ["free_gb"] = freeGb };

        if (freeBytes < UnhealthyThreshold)
            return Task.FromResult(HealthCheckResult.Unhealthy($"Critical: only {freeGb} GB free", data));
        if (freeBytes < DegradedThreshold)
            return Task.FromResult(HealthCheckResult.Degraded($"Low disk space: {freeGb} GB free", data));
        return Task.FromResult(HealthCheckResult.Healthy($"{freeGb} GB free", data));
    }
}
