namespace HealthEndpointPattern.Core;

public sealed record HealthReport(
    IReadOnlyDictionary<string, HealthCheckResult> Results,
    HealthStatus Status,
    TimeSpan TotalDuration
);
