using HealthEndpointPattern.Core;
using System.Diagnostics;

namespace HealthEndpointPattern.Registry;

public sealed class HealthCheckService
{
    private readonly List<IHealthCheck> _checks = [];

    public void Register(IHealthCheck check) => _checks.Add(check);

    public IReadOnlyList<IHealthCheck> Checks => _checks.AsReadOnly();

    public async Task<HealthReport> CheckHealthAsync(
        IEnumerable<string>? tags = null,
        CancellationToken ct = default)
    {
        var tagSet = tags?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var checksToRun = tagSet is null
            ? (IEnumerable<IHealthCheck>)_checks
            : _checks.Where(c => c.Tags.Any(t => tagSet.Contains(t)));

        var totalSw = Stopwatch.StartNew();

        var tasks = checksToRun.Select(async check =>
        {
            var sw = Stopwatch.StartNew();
            HealthCheckResult result;
            try
            {
                result = await check.CheckAsync(ct);
            }
            catch (Exception ex)
            {
                result = HealthCheckResult.Unhealthy($"Unhandled exception: {ex.Message}");
            }
            return (check.Name, Result: result with { Duration = sw.Elapsed });
        });

        var results = await Task.WhenAll(tasks);
        totalSw.Stop();

        IReadOnlyDictionary<string, HealthCheckResult> dict =
            results.ToDictionary(r => r.Name, r => r.Result);

        var overallStatus = dict.Count > 0
            ? (HealthStatus)dict.Values.Max(r => (int)r.Status)
            : HealthStatus.Healthy;

        return new HealthReport(dict, overallStatus, totalSw.Elapsed);
    }
}
