using HealthEndpointPattern.Checks;
using HealthEndpointPattern.Core;
using HealthEndpointPattern.Endpoints;
using HealthEndpointPattern.Registry;

Console.WriteLine("=== 4.30 Health Endpoint Monitoring — Maple Host ===\n");

// ─── 1. All healthy ───────────────────────────────────────────────────────────
Console.WriteLine("─── 1. GET /health — all dependencies healthy ───");

var svc1 = new HealthCheckService();
svc1.Register(new DatabaseHealthCheck());
svc1.Register(new ObjectStorageHealthCheck());
svc1.Register(new DiskSpaceHealthCheck(() => 45L * 1024 * 1024 * 1024));                    // 45 GB free
svc1.Register(new MemoryHealthCheck(() => (2L * 1024 * 1024 * 1024, 8L * 1024 * 1024 * 1024)));  // 25% used
svc1.Register(new ExternalApiHealthCheck());

await PrintAsync(new HealthEndpoint(svc1));

Pause();

// ─── 2. Degraded ─────────────────────────────────────────────────────────────
Console.WriteLine("─── 2. GET /health — disk space low (Degraded, HTTP 200) ───");

var svc2 = new HealthCheckService();
svc2.Register(new DatabaseHealthCheck());
svc2.Register(new ObjectStorageHealthCheck());
svc2.Register(new DiskSpaceHealthCheck(() => 5L * 1024 * 1024 * 1024));                     // 5 GB — degraded
svc2.Register(new MemoryHealthCheck(() => (2L * 1024 * 1024 * 1024, 8L * 1024 * 1024 * 1024)));
svc2.Register(new ExternalApiHealthCheck());

await PrintAsync(new HealthEndpoint(svc2));

Pause();

// ─── 3. Unhealthy ─────────────────────────────────────────────────────────────
Console.WriteLine("─── 3. GET /health — database down (Unhealthy, HTTP 503) ───");

var svc3 = new HealthCheckService();
svc3.Register(new DatabaseHealthCheck(() => false));                                          // DB is down
svc3.Register(new ObjectStorageHealthCheck());
svc3.Register(new DiskSpaceHealthCheck(() => 45L * 1024 * 1024 * 1024));
svc3.Register(new MemoryHealthCheck(() => (2L * 1024 * 1024 * 1024, 8L * 1024 * 1024 * 1024)));
svc3.Register(new ExternalApiHealthCheck());

await PrintAsync(new HealthEndpoint(svc3));

Pause();

// ─── 4. Liveness vs readiness ─────────────────────────────────────────────────
Console.WriteLine("─── 4. Liveness vs Readiness — filtered by tag ───");

// Database down (readiness-only → Unhealthy readiness, healthy liveness)
// Disk 5 GB (liveness + readiness → Degraded on both)
// Memory 25% (liveness-only → Healthy)
var svc4 = new HealthCheckService();
svc4.Register(new DatabaseHealthCheck(() => false));
svc4.Register(new ObjectStorageHealthCheck());
svc4.Register(new DiskSpaceHealthCheck(() => 5L * 1024 * 1024 * 1024));
svc4.Register(new MemoryHealthCheck(() => (2L * 1024 * 1024 * 1024, 8L * 1024 * 1024 * 1024)));
svc4.Register(new ExternalApiHealthCheck());

var ep4 = new HealthEndpoint(svc4);

Console.WriteLine("  GET /health/live  — restarts the container if 503:");
PrintReport(await ep4.GetLivenessAsync(), "    ");

Console.WriteLine("\n  GET /health/ready — removes from load balancer if 503:");
PrintReport(await ep4.GetReadinessAsync(), "    ");

static async Task PrintAsync(HealthEndpoint ep)
{
    var r = await ep.GetAsync();
    PrintReport(r, "  ");
}

static void PrintReport(HealthEndpointResponse r, string indent)
{
    Console.WriteLine($"{indent}HTTP {r.StatusCode}  Overall: {Icon(r.Report.Status)} {r.Report.Status}");
    foreach (var (name, result) in r.Report.Results.OrderBy(kv => kv.Key))
        Console.WriteLine($"{indent}  {Icon(result.Status)} {name,-20} {result.Status,-10}  {result.Description}");
}

static string Icon(HealthStatus s) => s switch
{
    HealthStatus.Healthy  => "✓",
    HealthStatus.Degraded => "⚠",
    _                     => "✗"
};

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
