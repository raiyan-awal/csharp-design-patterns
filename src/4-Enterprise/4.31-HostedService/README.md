# 4.31 — Hosted Service / Background Worker

## Intent

The Hosted Service pattern encapsulates a long-running background operation behind a pair of lifecycle methods — `StartAsync` and `StopAsync` — so the host (the .NET runtime, a test harness, or a `ServiceHost`) can start, monitor, and shut down background work in a controlled, cancellation-aware way. It decouples the "what to do in the background" concern from the "how to manage the lifetime of the process" concern.

## The Problem It Solves

Without a hosted service contract, background work is typically started ad hoc with no lifecycle management:

```csharp
// Without Hosted Service: fire-and-forget with no way to stop it cleanly
_ = Task.Run(async () =>
{
    while (true)
    {
        await CollectMetrics();
        await Task.Delay(TimeSpan.FromSeconds(30));
    }
});
// Problems:
// - No graceful shutdown: the task is abandoned when the process exits
// - No cancellation: it can't be stopped without killing the process
// - No coordination: multiple background tasks have no shared lifecycle
// - Unhandled exceptions crash the process or disappear silently
```

Problems this creates:
- **Data loss** — in-flight work (orders being processed, metrics being written) is interrupted abruptly on shutdown.
- **Uncoordinated startup** — without a registry, background tasks are scattered across startup code with no visibility into what is running.
- **No testability** — a bare `Task.Run` loop has no seam to inject test doubles, advance fake time, or assert on state between ticks.
- **Reverse-order shutdown is impossible** — dependencies started last should be stopped first; a list of loose tasks has no notion of registration order.

## Solution: `IHostedService` + `BackgroundService`

Each background concern implements `IHostedService` (directly, or by extending `BackgroundService`). The abstract `BackgroundService` class wires up `StartAsync` and `StopAsync` so concrete services only need to implement `ExecuteAsync`. A `ServiceHost` registry starts all services in registration order and stops them in reverse, mirroring the behaviour of `Microsoft.Extensions.Hosting.IHost`.

```csharp
var host = new ServiceHost();
host.Register(new HeartbeatService(interval: TimeSpan.FromSeconds(5)));
host.Register(new OrderProcessorService(channel, stats));
host.Register(new MetricsCollectorService(stats));

await host.StartAsync();   // starts all three in order

// ... application runs ...

await host.StopAsync();    // stops in reverse: metrics → processor → heartbeat
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Hosted service interface | `IHostedService` | `StartAsync` + `StopAsync` + `Name` |
| Abstract base | `BackgroundService` | Wires `IHostedService` to `ExecuteAsync`; manages `CancellationTokenSource`; swallows shutdown `OperationCanceledException` |
| Liveness worker | `HeartbeatService` | Emits a periodic ping at a configurable interval; injectable log action |
| Queue worker | `OrderProcessorService` | Reads `Order` records from a `Channel<Order>`; drains all queued items before stopping |
| Periodic worker | `MetricsCollectorService` | Snapshots `OrderStats` at a configurable interval; injectable clock |
| Order accumulator | `OrderStats` | Thread-safe revenue and count accumulator |
| Channel wrapper | `OrderChannel` | Typed `Channel<Order>` exposing separate `Writer` and `Reader` handles |
| Host registry | `ServiceHost` | Registers services; starts in order; stops in reverse |
| Domain record | `Order` | `Id`, `CustomerName`, `Item`, `AmountCAD` |
| Snapshot record | `MetricsSnapshot` | Point-in-time `OrderCount` + `TotalRevenueCAD` |

## Structure

```
4.31-HostedService/
├── HostedServicePattern/
│   ├── Core/
│   │   ├── IHostedService.cs             ← StartAsync / StopAsync / Name
│   │   └── BackgroundService.cs          ← abstract base; CTS management; OCE swallow
│   ├── Domain/
│   │   ├── Order.cs                      ← Id / CustomerName / Item / AmountCAD
│   │   └── MetricsSnapshot.cs            ← TakenAt / OrderCount / TotalRevenueCAD
│   ├── Infrastructure/
│   │   ├── OrderStats.cs                 ← thread-safe counter + revenue accumulator
│   │   └── OrderChannel.cs               ← Channel<Order> wrapper; Writer + Reader
│   ├── Services/
│   │   ├── HeartbeatService.cs           ← periodic pulse; injectable interval + log
│   │   ├── OrderProcessorService.cs      ← channel drain; TryComplete on stop
│   │   └── MetricsCollectorService.cs    ← periodic snapshot; injectable clock
│   ├── Host/
│   │   └── ServiceHost.cs                ← Register; StartAsync; StopAsync (reverse)
│   └── Program.cs
└── HostedServicePattern.Tests/
    └── HostedServicePatternTests.cs      ← 28 tests; ManualService + OrderedStopService stubs
```

## Key Code

### BackgroundService — the base class

```csharp
public abstract class BackgroundService : IHostedService
{
    private Task? _executeTask;
    private CancellationTokenSource? _cts;

    public virtual Task StartAsync(CancellationToken ct = default)
    {
        _cts         = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _executeTask = ExecuteAsync(_cts.Token);
        // Return immediately if already done; otherwise signal start complete
        // and let ExecuteAsync continue in the background.
        return _executeTask.IsCompleted ? _executeTask : Task.CompletedTask;
    }

    public virtual async Task StopAsync(CancellationToken ct = default)
    {
        if (_executeTask is null) return;
        try { _cts!.Cancel(); } catch (ObjectDisposedException) { }
        try { await _executeTask.ConfigureAwait(false); }
        catch (OperationCanceledException) { }   // expected — not a fault
    }
}
```

`StartAsync` fires `ExecuteAsync` without `await`, so the host can start multiple services in sequence without waiting for any one of them to finish. The linked `CancellationTokenSource` propagates both the host's shutdown token and the service's own `StopAsync` cancel into `ExecuteAsync`.

### HeartbeatService — simple periodic worker

```csharp
protected override async Task ExecuteAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        try { await Task.Delay(_interval, ct); }
        catch (OperationCanceledException) { return; }
        Interlocked.Increment(ref _pulses);
        _log($"[heartbeat] pulse #{_pulses}");
    }
}
```

`Task.Delay(_interval, ct)` is the canonical way to wait in a background loop — it is cancellable and yields the thread pool thread immediately. When the token is cancelled, `Task.Delay` throws `OperationCanceledException`; the `catch` exits the loop cleanly so `_executeTask` completes without faulting.

### OrderProcessorService — queue drain on graceful shutdown

```csharp
// ReadAllAsync(CancellationToken.None) drains all queued orders even after
// StopAsync is called; the writer is completed in StopAsync before the token
// is cancelled, so ReadAllAsync finishes naturally when the queue is empty.
protected override async Task ExecuteAsync(CancellationToken ct)
{
    await foreach (var order in _channel.Reader.ReadAllAsync(CancellationToken.None))
    {
        _stats.Record(order);
        Interlocked.Increment(ref _processed);
    }
}

public override async Task StopAsync(CancellationToken ct = default)
{
    _channel.Writer.TryComplete();   // seal the channel — no more writes allowed
    await base.StopAsync(ct);        // wait for drain to finish
}
```

Using `CancellationToken.None` in `ReadAllAsync` is the key to graceful drain. When `StopAsync` completes the writer, `ReadAllAsync` keeps processing remaining items until the queue is empty, then returns naturally — no items are dropped. Contrast this with passing the service's `ct`: that would abort mid-drain if the token is cancelled before the queue is empty.

### MetricsCollectorService — injectable clock for deterministic tests

```csharp
public sealed class MetricsCollectorService : BackgroundService
{
    private readonly Func<DateTimeOffset> _clock;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(_interval, ct); }
            catch (OperationCanceledException) { return; }
            var (count, total) = _stats.Read();
            lock (_lock) { _snapshots.Add(new MetricsSnapshot(_clock(), count, total)); }
        }
    }
}
```

The injected `Func<DateTimeOffset>` clock defaults to `DateTimeOffset.UtcNow` in production. Tests can inject a fixed time to assert exact `TakenAt` values without system clock noise.

### ServiceHost — registry with reverse-order shutdown

```csharp
public async Task StartAsync(CancellationToken ct = default)
{
    foreach (var svc in _services)
        await svc.StartAsync(ct);       // in registration order
}

public async Task StopAsync(CancellationToken ct = default)
{
    foreach (var svc in Enumerable.Reverse(_services))
        await svc.StopAsync(ct);        // in reverse — last registered, first stopped
}
```

Reverse-order shutdown mirrors how dependency injection containers tear down scopes: services registered last typically depend on services registered earlier, so they must be stopped first to avoid calling into a service that has already shut down.

## Demo Scenarios

```
1. HeartbeatService      — 300 ms interval; run ~1 s; stop gracefully; print pulse count
2. OrderProcessorService — enqueue 6 Canadian orders before start; stop drains all; print stats
3. MetricsCollectorService — 250 ms snapshots; orders arrive gradually; print snapshot history
4. ServiceHost           — all three services registered; 900 ms run; unified stop; final stats
```

## When to Use

- Periodic work: cache warm-up, metrics collection, scheduled report generation.
- Queue processors: reading from a `Channel<T>`, a message bus, or a database outbox.
- Long-lived listeners: watching a file system, polling an external API, maintaining a WebSocket connection.
- Any background task that must shut down cleanly — draining in-flight work before the process exits.

## When NOT to Use

- Short one-shot tasks — use `Task.Run` or `BackgroundJob` (Hangfire) instead; a hosted service adds lifecycle overhead that is only worth it for perpetual workers.
- CPU-bound batch jobs — they are better modelled as queued work items dispatched to a thread pool, not as a long-running `while` loop.
- When Microsoft.Extensions.Hosting is already in the stack — use the built-in `BackgroundService` and `IHostedService` from the framework instead of rolling your own.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Graceful shutdown | `StopAsync` cancels the token and awaits the task — in-flight work finishes before the process exits |
| Ordered lifecycle | `ServiceHost` starts in registration order and stops in reverse — dependency ordering is preserved |
| Testability | Injectable intervals, log actions, and clocks let tests drive all timing deterministically |
| Separation of concerns | Each service encapsulates one background concern; the host knows nothing about what they do |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| No built-in retry | If `ExecuteAsync` throws an unhandled exception the task faults and the loop stops; callers must add their own try/catch and back-off logic |
| Sequential stop | `ServiceHost.StopAsync` stops services one at a time in sequence; for many services with long drain times, a parallel stop with a shared timeout is more production-appropriate |
| No dependency injection | This demo wires dependencies manually; in real .NET apps `IHostedService` implementations are registered in `IServiceCollection` and resolved by the DI container |

## Related Patterns

- **Outbox Pattern (4.20)** — the Outbox relay is a natural hosted service: a periodic worker that reads unprocessed outbox messages and publishes them, with graceful drain on shutdown.
- **Health Endpoint Monitoring (4.30)** — a hosted service can expose its running state (heartbeat count, queue depth, last snapshot time) as data for health checks.
- **Rate Limiting (4.29)** — a background worker that resets or refills rate limit counters at a fixed interval is a hosted service use case.
- **Publish-Subscribe (4.24)** — a background subscriber that reads from a bus and fans out to handlers is typically implemented as a hosted service.

## Running the Demo

```bash
cd src/4-Enterprise/4.31-HostedService/HostedServicePattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.31-HostedService/HostedServicePattern.Tests
dotnet test
```
