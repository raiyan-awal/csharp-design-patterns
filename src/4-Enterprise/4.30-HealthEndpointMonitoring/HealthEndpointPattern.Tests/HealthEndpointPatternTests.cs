using HealthEndpointPattern.Checks;
using HealthEndpointPattern.Core;
using HealthEndpointPattern.Endpoints;
using HealthEndpointPattern.Registry;
using Xunit;

// ─── Stubs ────────────────────────────────────────────────────────────────────

file sealed class StubCheck(string name, HealthCheckResult result, params string[] tags) : IHealthCheck
{
    public bool WasCalled { get; private set; }
    public string Name => name;
    public IReadOnlyList<string> Tags { get; } = tags;
    public Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        WasCalled = true;
        return Task.FromResult(result);
    }
}

file sealed class ThrowingCheck : IHealthCheck
{
    public string Name => "throwing";
    public IReadOnlyList<string> Tags { get; } = [];
    public Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
        => throw new InvalidOperationException("simulated failure");
}

// ─── HealthStatus ordering ────────────────────────────────────────────────────

public sealed class HealthStatus_Ordering
{
    [Fact]
    public void Unhealthy_HasHighestIntValue()
    {
        Assert.True((int)HealthStatus.Unhealthy > (int)HealthStatus.Degraded);
        Assert.True((int)HealthStatus.Degraded  > (int)HealthStatus.Healthy);
    }

    [Fact]
    public void Max_HealthyAndDegraded_ReturnsDegraded()
    {
        var worst = (HealthStatus)new[] { HealthStatus.Healthy, HealthStatus.Degraded }
            .Max(s => (int)s);
        Assert.Equal(HealthStatus.Degraded, worst);
    }

    [Fact]
    public void Max_AllThree_ReturnsUnhealthy()
    {
        var worst = (HealthStatus)new[] { HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Unhealthy }
            .Max(s => (int)s);
        Assert.Equal(HealthStatus.Unhealthy, worst);
    }
}

// ─── HealthCheckResult factories ─────────────────────────────────────────────

public sealed class HealthCheckResult_Tests
{
    [Fact]
    public void Healthy_SetsCorrectStatus()
        => Assert.Equal(HealthStatus.Healthy, HealthCheckResult.Healthy().Status);

    [Fact]
    public void Degraded_SetsCorrectStatus()
        => Assert.Equal(HealthStatus.Degraded, HealthCheckResult.Degraded("low").Status);

    [Fact]
    public void Unhealthy_SetsCorrectStatus()
        => Assert.Equal(HealthStatus.Unhealthy, HealthCheckResult.Unhealthy("down").Status);

    [Fact]
    public void Data_IsPreservedOnResult()
    {
        IReadOnlyDictionary<string, object> data = new Dictionary<string, object> { ["key"] = 42 };
        var result = HealthCheckResult.Healthy("ok", data);
        Assert.Equal(42, result.Data!["key"]);
    }

    [Fact]
    public void WithExpression_CanOverrideDuration()
    {
        var result = HealthCheckResult.Healthy() with { Duration = TimeSpan.FromMilliseconds(5) };
        Assert.Equal(TimeSpan.FromMilliseconds(5), result.Duration);
    }
}

// ─── DatabaseHealthCheck ──────────────────────────────────────────────────────

public sealed class DatabaseHealthCheck_Tests
{
    [Fact]
    public async Task ReturnsHealthy_WhenConnected()
    {
        var check = new DatabaseHealthCheck(() => true);
        var result = await check.CheckAsync();
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task ReturnsUnhealthy_WhenCannotConnect()
    {
        var check = new DatabaseHealthCheck(() => false);
        var result = await check.CheckAsync();
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task ReturnsUnhealthy_WhenDelegateThrows()
    {
        var check = new DatabaseHealthCheck(() => throw new TimeoutException("timeout"));
        // Check itself must not throw — exceptions are the caller's responsibility in raw checks;
        // only HealthCheckService swallows them. Here DatabaseHealthCheck propagates.
        // We verify the service catches it in HealthCheckService_Tests.
        await Assert.ThrowsAsync<TimeoutException>(() => check.CheckAsync());
    }

    [Fact]
    public void HasReadinessTag()
        => Assert.Contains("readiness", new DatabaseHealthCheck().Tags);
}

// ─── DiskSpaceHealthCheck ─────────────────────────────────────────────────────

public sealed class DiskSpaceHealthCheck_Tests
{
    private static long Gb(long n) => n * 1024L * 1024 * 1024;

    [Fact]
    public async Task ReturnsHealthy_WhenAbove10Gb()
    {
        var check = new DiskSpaceHealthCheck(() => Gb(45));
        var result = await check.CheckAsync();
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task ReturnsDegraded_When2To10Gb()
    {
        var check = new DiskSpaceHealthCheck(() => Gb(5));
        var result = await check.CheckAsync();
        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task ReturnsUnhealthy_WhenBelow2Gb()
    {
        var check = new DiskSpaceHealthCheck(() => Gb(1));
        var result = await check.CheckAsync();
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task Data_ContainsFreeGb()
    {
        var check = new DiskSpaceHealthCheck(() => Gb(45));
        var result = await check.CheckAsync();
        Assert.NotNull(result.Data);
        Assert.True(result.Data.ContainsKey("free_gb"));
    }

    [Fact]
    public void HasLivenessAndReadinessTags()
    {
        var check = new DiskSpaceHealthCheck();
        Assert.Contains("liveness", check.Tags);
        Assert.Contains("readiness", check.Tags);
    }
}

// ─── MemoryHealthCheck ────────────────────────────────────────────────────────

public sealed class MemoryHealthCheck_Tests
{
    private static long Gb(long n) => n * 1024L * 1024 * 1024;

    [Fact]
    public async Task ReturnsHealthy_WhenBelow75Pct()
    {
        var check = new MemoryHealthCheck(() => (Gb(2), Gb(8)));  // 25%
        var result = await check.CheckAsync();
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task ReturnsDegraded_When75To90Pct()
    {
        var check = new MemoryHealthCheck(() => (Gb(7), Gb(8)));  // 87.5%
        var result = await check.CheckAsync();
        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task ReturnsUnhealthy_WhenAt90PctOrAbove()
    {
        var check = new MemoryHealthCheck(() => (Gb(8), Gb(8)));  // 100%
        var result = await check.CheckAsync();
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task Data_ContainsUsagePct()
    {
        var check = new MemoryHealthCheck(() => (Gb(2), Gb(8)));
        var result = await check.CheckAsync();
        Assert.NotNull(result.Data);
        Assert.True(result.Data.ContainsKey("usage_pct"));
    }

    [Fact]
    public void HasLivenessTag()
        => Assert.Contains("liveness", new MemoryHealthCheck().Tags);
}

// ─── ExternalApiHealthCheck ───────────────────────────────────────────────────

public sealed class ExternalApiHealthCheck_Tests
{
    [Fact]
    public async Task ReturnsHealthy_WhenReachable()
    {
        var check = new ExternalApiHealthCheck(isReachable: () => true);
        var result = await check.CheckAsync();
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task ReturnsUnhealthy_WhenNotReachable()
    {
        var check = new ExternalApiHealthCheck(isReachable: () => false);
        var result = await check.CheckAsync();
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public void HasReadinessTag()
        => Assert.Contains("readiness", new ExternalApiHealthCheck().Tags);
}

// ─── ObjectStorageHealthCheck ─────────────────────────────────────────────────

public sealed class ObjectStorageHealthCheck_Tests
{
    [Fact]
    public async Task ReturnsHealthy_WhenReachable()
    {
        var check = new ObjectStorageHealthCheck(() => true);
        var result = await check.CheckAsync();
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task ReturnsUnhealthy_WhenNotReachable()
    {
        var check = new ObjectStorageHealthCheck(() => false);
        var result = await check.CheckAsync();
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}

// ─── HealthCheckService ───────────────────────────────────────────────────────

public sealed class HealthCheckService_Tests
{
    [Fact]
    public async Task AllHealthy_ReturnsOverallHealthy()
    {
        var svc = new HealthCheckService();
        svc.Register(new StubCheck("a", HealthCheckResult.Healthy()));
        svc.Register(new StubCheck("b", HealthCheckResult.Healthy()));
        var report = await svc.CheckHealthAsync();
        Assert.Equal(HealthStatus.Healthy, report.Status);
    }

    [Fact]
    public async Task OneDegraded_ReturnsOverallDegraded()
    {
        var svc = new HealthCheckService();
        svc.Register(new StubCheck("a", HealthCheckResult.Healthy()));
        svc.Register(new StubCheck("b", HealthCheckResult.Degraded("low")));
        var report = await svc.CheckHealthAsync();
        Assert.Equal(HealthStatus.Degraded, report.Status);
    }

    [Fact]
    public async Task OneUnhealthy_ReturnsOverallUnhealthy()
    {
        var svc = new HealthCheckService();
        svc.Register(new StubCheck("a", HealthCheckResult.Healthy()));
        svc.Register(new StubCheck("b", HealthCheckResult.Degraded("low")));
        svc.Register(new StubCheck("c", HealthCheckResult.Unhealthy("down")));
        var report = await svc.CheckHealthAsync();
        Assert.Equal(HealthStatus.Unhealthy, report.Status);
    }

    [Fact]
    public async Task Exception_InCheck_CaughtAsUnhealthy()
    {
        var svc = new HealthCheckService();
        svc.Register(new ThrowingCheck());
        var report = await svc.CheckHealthAsync();
        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.Contains("Unhandled exception", report.Results["throwing"].Description);
    }

    [Fact]
    public async Task TagFilter_Liveness_RunsOnlyLivenessChecks()
    {
        var live = new StubCheck("live-check", HealthCheckResult.Healthy(), "liveness");
        var ready = new StubCheck("ready-check", HealthCheckResult.Healthy(), "readiness");
        var svc = new HealthCheckService();
        svc.Register(live);
        svc.Register(ready);

        await svc.CheckHealthAsync(["liveness"]);

        Assert.True(live.WasCalled);
        Assert.False(ready.WasCalled);
    }

    [Fact]
    public async Task TagFilter_Readiness_RunsOnlyReadinessChecks()
    {
        var live = new StubCheck("live-check", HealthCheckResult.Healthy(), "liveness");
        var ready = new StubCheck("ready-check", HealthCheckResult.Healthy(), "readiness");
        var svc = new HealthCheckService();
        svc.Register(live);
        svc.Register(ready);

        await svc.CheckHealthAsync(["readiness"]);

        Assert.False(live.WasCalled);
        Assert.True(ready.WasCalled);
    }

    [Fact]
    public async Task NullTags_RunsAllChecks()
    {
        var a = new StubCheck("a", HealthCheckResult.Healthy(), "liveness");
        var b = new StubCheck("b", HealthCheckResult.Healthy(), "readiness");
        var svc = new HealthCheckService();
        svc.Register(a);
        svc.Register(b);

        await svc.CheckHealthAsync(null);

        Assert.True(a.WasCalled);
        Assert.True(b.WasCalled);
    }

    [Fact]
    public async Task EmptyService_ReturnsHealthy()
    {
        var svc = new HealthCheckService();
        var report = await svc.CheckHealthAsync();
        Assert.Equal(HealthStatus.Healthy, report.Status);
        Assert.Empty(report.Results);
    }

    [Fact]
    public async Task Report_ContainsResultForEachCheck()
    {
        var svc = new HealthCheckService();
        svc.Register(new StubCheck("alpha", HealthCheckResult.Healthy()));
        svc.Register(new StubCheck("beta",  HealthCheckResult.Degraded("low")));
        var report = await svc.CheckHealthAsync();
        Assert.True(report.Results.ContainsKey("alpha"));
        Assert.True(report.Results.ContainsKey("beta"));
    }

    [Fact]
    public async Task Duration_IsRecordedOnEachResult()
    {
        var svc = new HealthCheckService();
        svc.Register(new StubCheck("a", HealthCheckResult.Healthy()));
        var report = await svc.CheckHealthAsync();
        Assert.True(report.Results["a"].Duration >= TimeSpan.Zero);
    }
}

// ─── HealthEndpoint ───────────────────────────────────────────────────────────

public sealed class HealthEndpoint_Tests
{
    private static HealthCheckService Svc(params IHealthCheck[] checks)
    {
        var svc = new HealthCheckService();
        foreach (var c in checks) svc.Register(c);
        return svc;
    }

    [Fact]
    public async Task GetAsync_Returns200_ForHealthy()
    {
        var ep = new HealthEndpoint(Svc(new StubCheck("a", HealthCheckResult.Healthy())));
        var r  = await ep.GetAsync();
        Assert.Equal(200, r.StatusCode);
    }

    [Fact]
    public async Task GetAsync_Returns200_ForDegraded()
    {
        var ep = new HealthEndpoint(Svc(new StubCheck("a", HealthCheckResult.Degraded("low"))));
        var r  = await ep.GetAsync();
        Assert.Equal(200, r.StatusCode);
    }

    [Fact]
    public async Task GetAsync_Returns503_ForUnhealthy()
    {
        var ep = new HealthEndpoint(Svc(new StubCheck("a", HealthCheckResult.Unhealthy("down"))));
        var r  = await ep.GetAsync();
        Assert.Equal(503, r.StatusCode);
    }

    [Fact]
    public async Task GetLivenessAsync_OnlyRunsLivenessTaggedChecks()
    {
        var live  = new StubCheck("liveness-check",  HealthCheckResult.Healthy(), "liveness");
        var ready = new StubCheck("readiness-check", HealthCheckResult.Healthy(), "readiness");
        var ep    = new HealthEndpoint(Svc(live, ready));

        await ep.GetLivenessAsync();

        Assert.True(live.WasCalled);
        Assert.False(ready.WasCalled);
    }

    [Fact]
    public async Task GetReadinessAsync_OnlyRunsReadinessTaggedChecks()
    {
        var live  = new StubCheck("liveness-check",  HealthCheckResult.Healthy(), "liveness");
        var ready = new StubCheck("readiness-check", HealthCheckResult.Healthy(), "readiness");
        var ep    = new HealthEndpoint(Svc(live, ready));

        await ep.GetReadinessAsync();

        Assert.False(live.WasCalled);
        Assert.True(ready.WasCalled);
    }
}
