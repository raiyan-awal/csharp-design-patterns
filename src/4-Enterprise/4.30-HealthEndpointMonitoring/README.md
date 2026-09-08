# 4.30 — Health Endpoint Monitoring

## Intent

Health Endpoint Monitoring exposes dedicated endpoints (`/health`, `/health/live`, `/health/ready`) that external orchestrators, load balancers, and monitoring tools can call to determine whether a service is alive, ready to accept traffic, and fully operational. Each endpoint runs a set of health checks against dependencies and returns a structured report with an HTTP status code that drives automated recovery actions.

## The Problem It Solves

Without dedicated health endpoints, external systems have no visibility into a service's internal state:

```csharp
// Without Health Endpoint Monitoring
public class ApiController
{
    public IActionResult Get() => Ok(_service.DoWork());
    // The load balancer sends traffic here regardless of:
    // - whether the database connection pool is exhausted
    // - whether the object storage bucket is unreachable
    // - whether the disk is nearly full
    // All of these silently degrade or fail individual requests with no
    // automated remediation — no pod restart, no traffic reroute.
}
```

Problems this creates:
- **Silent degradation** — a failing dependency causes request errors, but the platform sees the process as "alive" and does nothing.
- **No differentiation** — there is no way to distinguish "process crashed" (restart it) from "dependency unavailable" (drain traffic, keep the process running).
- **Manual intervention** — on-call engineers must notice rising error rates, diagnose the root cause, and manually drain or restart the instance.
- **Cascading impact** — a single unhealthy instance continues to receive traffic, spreading errors across callers until someone intervenes.

## Solution: Structured Health Checks Behind Named Endpoints

Each dependency check implements `IHealthCheck` and returns `HealthCheckResult.Healthy`, `.Degraded`, or `.Unhealthy`. `HealthCheckService` runs all registered checks concurrently, catches any exceptions, and aggregates results into a `HealthReport` whose overall status is the worst of all individual statuses. `HealthEndpoint` maps that report to an HTTP response — 200 for Healthy or Degraded, 503 for Unhealthy — and supports tag-filtered probes for liveness and readiness.

```csharp
var service = new HealthCheckService();
service.Register(new DatabaseHealthCheck());
service.Register(new DiskSpaceHealthCheck());
service.Register(new MemoryHealthCheck());

var endpoint = new HealthEndpoint(service);

// GET /health/live — only "liveness" tagged checks
var liveness = await endpoint.GetLivenessAsync();   // 200 or 503

// GET /health/ready — only "readiness" tagged checks
var readiness = await endpoint.GetReadinessAsync(); // 200 or 503
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Health check interface | `IHealthCheck` | Contract: `CheckAsync()` + `Name` + `Tags` |
| Health check result | `HealthCheckResult` | Immutable result: `Status`, `Description`, `Duration`, optional `Data` |
| Health report | `HealthReport` | Aggregated results dictionary + overall `Status` + `TotalDuration` |
| Status enum | `HealthStatus` | `Healthy = 0`, `Degraded = 1`, `Unhealthy = 2` — ordered for `Max()` aggregation |
| Database check | `DatabaseHealthCheck` | Verifies PostgreSQL connectivity via injectable delegate; tagged `readiness` |
| Object storage check | `ObjectStorageHealthCheck` | Verifies object storage bucket reachability; tagged `readiness` |
| Disk space check | `DiskSpaceHealthCheck` | Checks free disk space against 10 GB / 2 GB thresholds; tagged `liveness` + `readiness` |
| Memory check | `MemoryHealthCheck` | Checks memory usage against 75% / 90% thresholds; tagged `liveness` |
| External API check | `ExternalApiHealthCheck` | Verifies third-party API reachability; tagged `readiness` |
| Health check service | `HealthCheckService` | Registers checks; runs them concurrently; catches exceptions; filters by tag |
| Health endpoint | `HealthEndpoint` | Simulates `GET /health`, `/health/live`, `/health/ready`; maps Unhealthy → 503 |
| Endpoint response | `HealthEndpointResponse` | HTTP status code + `HealthReport` pair |

## Structure

```
4.30-HealthEndpointMonitoring/
├── HealthEndpointPattern/
│   ├── Core/
│   │   ├── IHealthCheck.cs              ← CheckAsync() + Name / Tags
│   │   ├── HealthCheckResult.cs         ← Healthy / Degraded / Unhealthy factories; Duration; Data
│   │   ├── HealthReport.cs              ← aggregated Results + overall Status + TotalDuration
│   │   └── HealthStatus.cs              ← Healthy=0, Degraded=1, Unhealthy=2
│   ├── Checks/
│   │   ├── DatabaseHealthCheck.cs       ← injectable Func<bool>; tags: readiness
│   │   ├── ObjectStorageHealthCheck.cs  ← injectable Func<bool>; tags: readiness
│   │   ├── DiskSpaceHealthCheck.cs      ← injectable Func<long>; thresholds 10 GB / 2 GB; tags: liveness + readiness
│   │   ├── MemoryHealthCheck.cs         ← injectable Func<(long,long)>; thresholds 75% / 90%; tags: liveness
│   │   └── ExternalApiHealthCheck.cs    ← injectable Func<bool>; tags: readiness
│   ├── Registry/
│   │   └── HealthCheckService.cs        ← Register; CheckHealthAsync with tag filtering; Task.WhenAll
│   ├── Endpoints/
│   │   └── HealthEndpoint.cs            ← GetAsync / GetLivenessAsync / GetReadinessAsync; 503 on Unhealthy
│   └── Program.cs
└── HealthEndpointPattern.Tests/
    └── HealthEndpointPatternTests.cs    ← 42 tests; file-scoped StubCheck + ThrowingCheck stubs
```

## Key Code

### IHealthCheck — the contract

```csharp
public interface IHealthCheck
{
    string Name { get; }
    IReadOnlyList<string> Tags { get; }
    Task<HealthCheckResult> CheckAsync(CancellationToken ct = default);
}
```

Every dependency check implements this interface. `Tags` categorises the check — `"liveness"` for checks that determine if the process should be restarted, `"readiness"` for checks that determine whether it should receive traffic. A check can carry both tags (e.g. `DiskSpaceHealthCheck`) when low disk affects both probes.

### HealthStatus enum — ordered for aggregation

```csharp
public enum HealthStatus { Healthy = 0, Degraded = 1, Unhealthy = 2 }
```

The integer values are deliberately ordered from best to worst so that `Max()` over a collection of statuses returns the worst one without any custom comparer.

### HealthCheckResult — immutable result with optional data

```csharp
public static HealthCheckResult Healthy(string description = "Healthy",
    IReadOnlyDictionary<string, object>? data = null)
    => new() { Status = HealthStatus.Healthy, Description = description, Data = data };

public static HealthCheckResult Degraded(string description, ...) => ...;
public static HealthCheckResult Unhealthy(string description, ...) => ...;
```

The `Data` dictionary carries check-specific metrics (e.g. `free_gb`, `usage_pct`) for monitoring dashboards without polluting the `Description` string. `Duration` is set by `HealthCheckService` via a `with` expression after the check returns, so each check doesn't need to time itself.

### HealthCheckService — concurrent execution, exception safety, tag filtering

```csharp
public async Task<HealthReport> CheckHealthAsync(
    IEnumerable<string>? tags = null,
    CancellationToken ct = default)
{
    var tagSet = tags?.ToHashSet(StringComparer.OrdinalIgnoreCase);
    var checksToRun = tagSet is null
        ? (IEnumerable<IHealthCheck>)_checks
        : _checks.Where(c => c.Tags.Any(t => tagSet.Contains(t)));

    var tasks = checksToRun.Select(async check =>
    {
        var sw = Stopwatch.StartNew();
        HealthCheckResult result;
        try   { result = await check.CheckAsync(ct); }
        catch (Exception ex)
        { result = HealthCheckResult.Unhealthy($"Unhandled exception: {ex.Message}"); }
        return (check.Name, Result: result with { Duration = sw.Elapsed });
    });

    var results  = await Task.WhenAll(tasks);
    var overall  = (HealthStatus)results.Max(r => (int)r.Result.Status);
    ...
}
```

`Task.WhenAll` runs all checks in parallel — a slow check does not block a fast one. Exceptions are caught and treated as `Unhealthy` so one crashing check does not abort the entire report. The empty-service case returns `Healthy` (no failing checks).

### HealthEndpoint — HTTP status mapping and tag-filtered probes

```csharp
public async Task<HealthEndpointResponse> GetLivenessAsync(CancellationToken ct = default)
    => ToResponse(await service.CheckHealthAsync(["liveness"], ct));

public async Task<HealthEndpointResponse> GetReadinessAsync(CancellationToken ct = default)
    => ToResponse(await service.CheckHealthAsync(["readiness"], ct));

private static HealthEndpointResponse ToResponse(HealthReport report) =>
    new(report.Status == HealthStatus.Unhealthy ? 503 : 200, report);
```

`Degraded` maps to 200 by design: the service is operational but under stress. An orchestrator seeing 200 keeps the instance in rotation; operators monitoring the detailed report can see the degradation and act before it becomes an outage. `Unhealthy` maps to 503, triggering automated remediation (pod restart for liveness, traffic drain for readiness).

## Demo Scenarios

```
1. All healthy          — 5 checks all green; HTTP 200; full report printed
2. Disk space low       — DiskSpaceHealthCheck degraded (5 GB free); overall Degraded; HTTP 200
3. Database down        — DatabaseHealthCheck unhealthy; overall Unhealthy; HTTP 503
4. Liveness vs readiness — filtered probes: /health/live runs disk+memory only;
                           /health/ready runs db+storage+disk+external-api
```

## When to Use

- Kubernetes or ECS liveness probes (restart the container when the process is stuck or deadlocked).
- Kubernetes readiness probes (remove the pod from the Service's endpoint list when a dependency is unavailable).
- Load balancer active health checks to stop routing traffic to degraded instances.
- On-call dashboards and alerting pipelines that need structured dependency status, not just "is the port open."

## When NOT to Use

- When the checks themselves are expensive (full database queries, large file reads) and called at high frequency — cache the result or use a background refresh instead of running on every probe.
- When you need rich observability — health endpoints answer "yes/no/degraded" questions; for metrics, traces, and histograms use OpenTelemetry.
- When health endpoint responses might leak sensitive internal details (internal hostnames, stack traces, connection strings) — keep descriptions safe for public or ops-facing exposure.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Automated recovery | Kubernetes restarts pods that fail liveness; drains pods that fail readiness — without manual intervention |
| Dependency visibility | Operators see which specific dependency degraded before error rates spike |
| Probe separation | Liveness and readiness have different remediation paths — the endpoint model encodes that distinction directly |
| Testable without I/O | Injectable delegates let tests control every check outcome without real databases or network calls |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Check overhead | Every probe call runs checks; expensive round-trips (DB, external API) add latency to the probe response itself |
| False positives | An overly sensitive check (Unhealthy on first transient error) can trigger unnecessary pod restarts; checks should reflect sustained failure, not individual hiccups |
| Information exposure | Description strings in the report are visible to whoever can reach the endpoint; avoid including stack traces, internal hostnames, or credentials |

## Related Patterns

- **Circuit Breaker (4.16)** — the circuit breaker protects callers from an unhealthy downstream service; the health endpoint exposes that unhealthy state to the orchestrator so it can stop sending traffic to this instance entirely.
- **Retry Pattern (4.17)** — a load balancer seeing a 503 readiness response may retry the request on another healthy instance; the health endpoint is what makes that routing decision possible.
- **Rate Limiting (4.29)** — rate limiters should exempt the `/health` path so orchestrators always receive a timely response even during heavy traffic.
- **Bulkhead (4.18)** — bulkheads isolate dependency capacity; health checks can report on bulkhead saturation (high queue depth or low available permits) as a `Degraded` signal.

## Running the Demo

```bash
cd src/4-Enterprise/4.30-HealthEndpointMonitoring/HealthEndpointPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.30-HealthEndpointMonitoring/HealthEndpointPattern.Tests
dotnet test
```
