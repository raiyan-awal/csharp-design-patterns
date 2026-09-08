using HealthEndpointPattern.Core;

namespace HealthEndpointPattern.Checks;

public sealed class MemoryHealthCheck : IHealthCheck
{
    private readonly Func<(long UsedBytes, long TotalBytes)> _getMemory;

    public MemoryHealthCheck(Func<(long, long)>? getMemory = null)
        => _getMemory = getMemory ?? (() => (1L * 1024 * 1024 * 1024, 8L * 1024 * 1024 * 1024));

    public string Name => "memory";
    public IReadOnlyList<string> Tags { get; } = ["liveness"];

    public Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var (used, total) = _getMemory();
        var pct     = total > 0 ? (double)used / total * 100 : 0;
        var usedMb  = Math.Round(used  / (1024.0 * 1024), 0);
        var totalMb = Math.Round(total / (1024.0 * 1024), 0);
        IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
        {
            ["used_mb"]    = usedMb,
            ["total_mb"]   = totalMb,
            ["usage_pct"]  = Math.Round(pct, 1)
        };

        if (pct >= 90)
            return Task.FromResult(HealthCheckResult.Unhealthy($"Memory critical: {pct:F0}% used", data));
        if (pct >= 75)
            return Task.FromResult(HealthCheckResult.Degraded($"Memory pressure: {pct:F0}% used", data));
        return Task.FromResult(HealthCheckResult.Healthy($"{pct:F0}% used ({usedMb} / {totalMb} MB)", data));
    }
}
