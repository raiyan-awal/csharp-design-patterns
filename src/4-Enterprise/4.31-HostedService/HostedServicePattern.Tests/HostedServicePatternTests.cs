using HostedServicePattern.Core;
using HostedServicePattern.Domain;
using HostedServicePattern.Host;
using HostedServicePattern.Infrastructure;
using HostedServicePattern.Services;
using Xunit;

// ─── Test stubs ───────────────────────────────────────────────────────────────

file sealed class ManualService(string name, Func<CancellationToken, Task>? body = null)
    : BackgroundService(name)
{
    protected override Task ExecuteAsync(CancellationToken ct)
        => body is null ? Task.Delay(Timeout.Infinite, ct) : body(ct);
}

file sealed class OrderedStopService : BackgroundService
{
    private readonly List<string> _log;
    public OrderedStopService(string name, List<string> log) : base(name) => _log = log;
    protected override Task ExecuteAsync(CancellationToken ct) => Task.Delay(Timeout.Infinite, ct);
    public override async Task StopAsync(CancellationToken ct = default)
    { await base.StopAsync(ct); _log.Add(Name); }
}

// ─── BackgroundService lifecycle ──────────────────────────────────────────────

public sealed class BackgroundService_Tests
{
    [Fact]
    public async Task StartAsync_BeginsExecution()
    {
        var started = new TaskCompletionSource();
        var svc = new ManualService("test", async ct =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, ct);
        });

        await svc.StartAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await svc.StopAsync();

        Assert.True(started.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task StopAsync_CancelsExecution()
    {
        var cancelled = new TaskCompletionSource();
        var svc = new ManualService("test", async ct =>
        {
            try { await Task.Delay(Timeout.Infinite, ct); }
            catch (OperationCanceledException) { cancelled.SetResult(); }
        });

        await svc.StartAsync();
        await svc.StopAsync();

        Assert.True(cancelled.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task StopAsync_BeforeStart_DoesNotThrow()
    {
        var svc = new ManualService("test");
        await svc.StopAsync();  // should not throw
    }

    [Fact]
    public async Task StartAsync_ReturnsFast_EvenIfExecuteIsLong()
    {
        var svc = new ManualService("test");  // runs indefinitely
        var sw  = System.Diagnostics.Stopwatch.StartNew();
        await svc.StartAsync();
        sw.Stop();
        await svc.StopAsync();

        Assert.True(sw.ElapsedMilliseconds < 200,
            $"StartAsync took {sw.ElapsedMilliseconds} ms — expected near-instant");
    }
}

// ─── HeartbeatService ─────────────────────────────────────────────────────────

public sealed class HeartbeatService_Tests
{
    [Fact]
    public void Pulses_StartAtZero()
        => Assert.Equal(0, new HeartbeatService().Pulses);

    [Fact]
    public async Task Pulses_IncrementAfterInterval()
    {
        var svc = new HeartbeatService(interval: TimeSpan.FromMilliseconds(20));
        await svc.StartAsync();
        await Task.Delay(200);   // 10× interval — conservatively expect ≥3
        await svc.StopAsync();
        Assert.True(svc.Pulses >= 3, $"Expected ≥3 pulses, got {svc.Pulses}");
    }

    [Fact]
    public async Task StopAsync_StopsPulses()
    {
        var svc = new HeartbeatService(interval: TimeSpan.FromMilliseconds(20));
        await svc.StartAsync();
        await Task.Delay(100);
        await svc.StopAsync();
        var countAtStop = svc.Pulses;
        await Task.Delay(100);   // additional wait — no more pulses expected
        Assert.Equal(countAtStop, svc.Pulses);
    }

    [Fact]
    public async Task LogAction_CalledOnEachPulse()
    {
        var log = new List<string>();
        var svc = new HeartbeatService(interval: TimeSpan.FromMilliseconds(20),
                                       log: msg => log.Add(msg));
        await svc.StartAsync();
        await Task.Delay(150);
        await svc.StopAsync();
        Assert.True(log.Count >= 3, $"Expected ≥3 log entries, got {log.Count}");
        Assert.All(log, msg => Assert.Contains("heartbeat", msg));
    }
}

// ─── OrderStats ───────────────────────────────────────────────────────────────

public sealed class OrderStats_Tests
{
    private static Order Make(decimal amt) => new("X", "Customer", "Item", amt);

    [Fact]
    public void Read_ReturnsZero_Initially()
    {
        var (count, total) = new OrderStats().Read();
        Assert.Equal(0, count);
        Assert.Equal(0m, total);
    }

    [Fact]
    public void Record_IncrementsCount()
    {
        var stats = new OrderStats();
        stats.Record(Make(10m));
        stats.Record(Make(20m));
        Assert.Equal(2, stats.Read().Count);
    }

    [Fact]
    public void Record_AccumulatesRevenue()
    {
        var stats = new OrderStats();
        stats.Record(Make(49.99m));
        stats.Record(Make(99.99m));
        Assert.Equal(149.98m, stats.Read().TotalCAD);
    }

    [Fact]
    public void Record_MultipleOrders_SumsCorrectly()
    {
        var stats = new OrderStats();
        decimal[] amounts = [10m, 20m, 30m, 40m, 50m];
        foreach (var a in amounts) stats.Record(Make(a));
        var (count, total) = stats.Read();
        Assert.Equal(5, count);
        Assert.Equal(150m, total);
    }
}

// ─── OrderChannel ─────────────────────────────────────────────────────────────

public sealed class OrderChannel_Tests
{
    [Fact]
    public async Task Write_ThenRead_ReturnsOrder()
    {
        var ch    = new OrderChannel();
        var order = new Order("O1", "Alice", "Widget", 19.99m);
        await ch.Writer.WriteAsync(order);
        var received = await ch.Reader.ReadAsync();
        Assert.Equal(order, received);
    }

    [Fact]
    public async Task TryComplete_AllowsDrain()
    {
        var ch = new OrderChannel();
        await ch.Writer.WriteAsync(new("O1", "A", "X", 1m));
        await ch.Writer.WriteAsync(new("O2", "B", "Y", 2m));
        ch.Writer.TryComplete();

        var read = new List<Order>();
        await foreach (var o in ch.Reader.ReadAllAsync())
            read.Add(o);

        Assert.Equal(2, read.Count);
    }
}

// ─── OrderProcessorService ────────────────────────────────────────────────────

public sealed class OrderProcessorService_Tests
{
    [Fact]
    public async Task ProcessedCount_StartsAtZero()
    {
        var svc = new OrderProcessorService(new OrderChannel(), new OrderStats());
        Assert.Equal(0, svc.ProcessedCount);
    }

    [Fact]
    public async Task ProcessesOrderFromChannel()
    {
        var ch    = new OrderChannel();
        var stats = new OrderStats();
        var svc   = new OrderProcessorService(ch, stats);

        await ch.Writer.WriteAsync(new("O1", "Lena", "Book", 24.99m));
        await svc.StartAsync();
        await svc.StopAsync();  // drains channel before stopping

        Assert.Equal(1, svc.ProcessedCount);
    }

    [Fact]
    public async Task ProcessedCount_MatchesEnqueuedOrders()
    {
        var ch    = new OrderChannel();
        var stats = new OrderStats();
        var svc   = new OrderProcessorService(ch, stats);

        for (var i = 0; i < 5; i++)
            await ch.Writer.WriteAsync(new($"O{i}", "C", "I", 10m));

        await svc.StartAsync();
        await svc.StopAsync();

        Assert.Equal(5, svc.ProcessedCount);
    }

    [Fact]
    public async Task StopAsync_DrainsRemainingOrders()
    {
        var ch    = new OrderChannel();
        var stats = new OrderStats();
        var svc   = new OrderProcessorService(ch, stats);

        // Enqueue before starting — all must be processed on drain
        for (var i = 0; i < 10; i++)
            await ch.Writer.WriteAsync(new($"O{i}", "C", "I", 1m));

        await svc.StartAsync();
        await svc.StopAsync();  // completes writer → drains all 10

        Assert.Equal(10, svc.ProcessedCount);
    }

    [Fact]
    public async Task Stats_UpdatedAfterProcessing()
    {
        var ch    = new OrderChannel();
        var stats = new OrderStats();
        var svc   = new OrderProcessorService(ch, stats);

        await ch.Writer.WriteAsync(new("O1", "X", "Y", 50m));
        await ch.Writer.WriteAsync(new("O2", "X", "Y", 75m));

        await svc.StartAsync();
        await svc.StopAsync();

        var (count, total) = stats.Read();
        Assert.Equal(2, count);
        Assert.Equal(125m, total);
    }
}

// ─── MetricsCollectorService ──────────────────────────────────────────────────

public sealed class MetricsCollectorService_Tests
{
    [Fact]
    public void Snapshots_StartEmpty()
        => Assert.Empty(new MetricsCollectorService(new OrderStats()).Snapshots);

    [Fact]
    public async Task TakesSnapshotAfterInterval()
    {
        var svc = new MetricsCollectorService(new OrderStats(),
                                              interval: TimeSpan.FromMilliseconds(10));
        await svc.StartAsync();
        await Task.Delay(150);   // 15× the interval
        await svc.StopAsync();
        Assert.True(svc.Snapshots.Count >= 3,
            $"Expected ≥3 snapshots, got {svc.Snapshots.Count}");
    }

    [Fact]
    public async Task SnapshotReflectsCurrentOrderStats()
    {
        var stats = new OrderStats();
        stats.Record(new("O1", "X", "Y", 99.99m));
        stats.Record(new("O2", "X", "Y", 49.99m));

        var svc = new MetricsCollectorService(stats, interval: TimeSpan.FromMilliseconds(10));
        await svc.StartAsync();
        await Task.Delay(50);
        await svc.StopAsync();

        Assert.True(svc.Snapshots.Count >= 1);
        var snap = svc.Snapshots.Last();
        Assert.Equal(2, snap.OrderCount);
        Assert.Equal(149.98m, snap.TotalRevenueCAD);
    }

    [Fact]
    public async Task StopAsync_StopsTakingSnapshots()
    {
        var svc = new MetricsCollectorService(new OrderStats(),
                                              interval: TimeSpan.FromMilliseconds(10));
        await svc.StartAsync();
        await Task.Delay(100);
        await svc.StopAsync();
        var countAtStop = svc.Snapshots.Count;
        await Task.Delay(100);
        Assert.Equal(countAtStop, svc.Snapshots.Count);
    }
}

// ─── ServiceHost ──────────────────────────────────────────────────────────────

public sealed class ServiceHost_Tests
{
    [Fact]
    public void Register_AddsService()
    {
        var host = new ServiceHost();
        host.Register(new ManualService("a"));
        Assert.Single(host.Services);
    }

    [Fact]
    public async Task StartAsync_StartsAllServices()
    {
        var started = new List<string>();
        var host    = new ServiceHost();
        host.Register(new ManualService("svc-a",
            async ct => { lock (started) started.Add("svc-a"); await Task.Delay(Timeout.Infinite, ct); }));
        host.Register(new ManualService("svc-b",
            async ct => { lock (started) started.Add("svc-b"); await Task.Delay(Timeout.Infinite, ct); }));

        await host.StartAsync();
        await Task.Delay(50);  // let ExecuteAsync begin
        await host.StopAsync();

        Assert.Contains("svc-a", started);
        Assert.Contains("svc-b", started);
    }

    [Fact]
    public async Task StopAsync_StopsAllServices()
    {
        var stopLog = new List<string>();
        var host    = new ServiceHost();
        host.Register(new OrderedStopService("a", stopLog));
        host.Register(new OrderedStopService("b", stopLog));

        await host.StartAsync();
        await host.StopAsync();

        Assert.Contains("a", stopLog);
        Assert.Contains("b", stopLog);
    }

    [Fact]
    public async Task StopAsync_StopsInReverseOrder()
    {
        var stopLog = new List<string>();
        var host    = new ServiceHost();
        host.Register(new OrderedStopService("first",  stopLog));
        host.Register(new OrderedStopService("second", stopLog));
        host.Register(new OrderedStopService("third",  stopLog));

        await host.StartAsync();
        await host.StopAsync();

        Assert.Equal(["third", "second", "first"], stopLog);
    }

    [Fact]
    public async Task FullPipeline_ProcessesOrdersAndCollectsMetrics()
    {
        var ch        = new OrderChannel();
        var stats     = new OrderStats();
        var host      = new ServiceHost();
        host.Register(new OrderProcessorService(ch, stats));
        host.Register(new MetricsCollectorService(stats, interval: TimeSpan.FromMilliseconds(20)));

        await host.StartAsync();
        await ch.Writer.WriteAsync(new("O1", "C", "I", 100m));
        await ch.Writer.WriteAsync(new("O2", "C", "I", 200m));
        await Task.Delay(100);
        await host.StopAsync();

        var (count, total) = stats.Read();
        Assert.Equal(2, count);
        Assert.Equal(300m, total);
    }
}
