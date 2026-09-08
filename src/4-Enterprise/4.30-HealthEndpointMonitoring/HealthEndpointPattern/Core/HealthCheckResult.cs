namespace HealthEndpointPattern.Core;

public sealed record HealthCheckResult
{
    public HealthStatus Status { get; init; }
    public string Description { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public IReadOnlyDictionary<string, object>? Data { get; init; }

    public static HealthCheckResult Healthy(string description = "Healthy",
        IReadOnlyDictionary<string, object>? data = null)
        => new() { Status = HealthStatus.Healthy, Description = description, Data = data };

    public static HealthCheckResult Degraded(string description,
        IReadOnlyDictionary<string, object>? data = null)
        => new() { Status = HealthStatus.Degraded, Description = description, Data = data };

    public static HealthCheckResult Unhealthy(string description,
        IReadOnlyDictionary<string, object>? data = null)
        => new() { Status = HealthStatus.Unhealthy, Description = description, Data = data };
}
