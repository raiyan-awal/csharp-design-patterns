using HealthEndpointPattern.Core;
using HealthEndpointPattern.Registry;

namespace HealthEndpointPattern.Endpoints;

public sealed record HealthEndpointResponse(int StatusCode, HealthReport Report);

public sealed class HealthEndpoint(HealthCheckService service)
{
    public async Task<HealthEndpointResponse> GetAsync(CancellationToken ct = default)
        => ToResponse(await service.CheckHealthAsync(ct: ct));

    public async Task<HealthEndpointResponse> GetLivenessAsync(CancellationToken ct = default)
        => ToResponse(await service.CheckHealthAsync(["liveness"], ct));

    public async Task<HealthEndpointResponse> GetReadinessAsync(CancellationToken ct = default)
        => ToResponse(await service.CheckHealthAsync(["readiness"], ct));

    private static HealthEndpointResponse ToResponse(HealthReport report) =>
        new(report.Status == HealthStatus.Unhealthy ? 503 : 200, report);
}
