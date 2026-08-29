# 4.18 — Bulkhead

## Intent

The Bulkhead Pattern isolates resources — thread slots, connection pools, semaphore permits — per downstream dependency, so that one slow or failing service cannot exhaust shared capacity and bring down unrelated parts of the system. The name comes from the watertight compartments in a ship's hull: one flooded compartment does not sink the vessel.

## The Problem It Solves

```csharp
// Without Bulkhead: all services share the same thread pool
// If AccountService slows to 30s per call, 200 threads queue up waiting for it.
// NetworkService and BillingService can no longer get threads — they appear dead
// even though they are perfectly healthy.
var account = accountService.GetAccount(id);     // blocks a thread for 30s
var network = networkService.GetStatus(region);  // no thread available — also blocked
var billing = billingService.GetBalance(id);     // no thread available — also blocked
```

Problems:

- **Thread starvation.** One slow downstream monopolises the thread pool. Other, healthy services queue behind it and appear unresponsive to callers.
- **Cascading failure.** A degraded Account Service causes Network and Billing calls to fail with timeout errors even though those services are healthy.
- **No isolation boundary.** Every service call competes for the same pool. There is no cap on how many concurrent calls any single dependency can hold.
- **All-or-nothing failure.** A partial outage in one dependency takes the entire application with it.

## Solution: Per-Dependency Semaphore

```csharp
var accountBulkhead = new BulkheadPolicy(new BulkheadOptions
{
    MaxConcurrency = 5,    // at most 5 simultaneous account service calls
    MaxQueueSize   = 10,   // up to 10 callers may wait for a free slot
    QueueTimeout   = TimeSpan.FromSeconds(2)
});

var networkBulkhead = new BulkheadPolicy(new BulkheadOptions
{
    MaxConcurrency = 3,    // independent limit — not shared with accountBulkhead
    MaxQueueSize   = 0     // no queue — reject immediately if all slots busy
});

// Account service saturated — throws BulkheadRejectedException
var account = accountBulkhead.Execute(() => accountService.GetAccount(id));

// Network service completely unaffected — its own semaphore has free slots
var network = networkBulkhead.Execute(() => networkService.GetStatus(region));
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Policy | `BulkheadPolicy` | Holds a `SemaphoreSlim`; gates concurrent access; queues or rejects overflow |
| Options | `BulkheadOptions` | `MaxConcurrency`, `MaxQueueSize`, `QueueTimeout` |
| Exception | `BulkheadRejectedException` | Thrown when all slots and queue positions are full, or the queue timeout expires |
| Service A | `SimulatedAccountService` | Maple Connect customer account lookups; controllable latency |
| Service B | `SimulatedNetworkService` | Maple Connect tower status; independent bulkhead in the demo |

## Structure

```
4.18-Bulkhead/
├── BulkheadPattern/
│   ├── Core/
│   │   ├── BulkheadOptions.cs            ← MaxConcurrency, MaxQueueSize, QueueTimeout
│   │   ├── BulkheadRejectedException.cs
│   │   └── BulkheadPolicy.cs             ← SemaphoreSlim; Available; Queued; Execute<T>
│   ├── Services/
│   │   ├── AccountInfo.cs                ← result record
│   │   ├── NetworkStatus.cs              ← result record
│   │   └── SimulatedTelecomServices.cs   ← SimulatedAccountService + SimulatedNetworkService
│   └── Program.cs                        ← 4-section demo
└── BulkheadPattern.Tests/
    └── BulkheadPolicyTests.cs            ← 15 tests across 4 suites
```

## Key Code

### BulkheadPolicy.Execute — semaphore gate with optional queue

```csharp
public T Execute<T>(Func<T> action)
{
    if (_options.MaxQueueSize == 0)
    {
        // No queue: reject immediately if all slots are busy
        if (!_semaphore.Wait(TimeSpan.Zero))
            throw new BulkheadRejectedException(
                $"Bulkhead saturated — all {_options.MaxConcurrency} execution slot(s) are busy.");
    }
    else
    {
        // Fast path: acquire without queuing if a slot is available
        if (!_semaphore.Wait(TimeSpan.Zero))
        {
            var position = Interlocked.Increment(ref _queuedCount);
            if (position > _options.MaxQueueSize)
            {
                Interlocked.Decrement(ref _queuedCount);
                throw new BulkheadRejectedException(
                    $"Bulkhead queue full — {_options.MaxQueueSize} caller(s) already waiting.");
            }

            try
            {
                if (!_semaphore.Wait(_options.QueueTimeout))
                    throw new BulkheadRejectedException(
                        "Bulkhead queue timeout — caller waited too long.");
            }
            finally
            {
                Interlocked.Decrement(ref _queuedCount);
            }
        }
    }

    try   { return action(); }
    finally { _semaphore.Release(); }
}
```

`SemaphoreSlim.Wait(TimeSpan.Zero)` is the non-blocking fast path — it either acquires instantly or returns false. The `finally { _semaphore.Release(); }` guarantees the slot is always returned, even if the action throws.

### Two-mode behaviour controlled by MaxQueueSize

```
MaxQueueSize = 0  →  Fail-fast bulkhead
  Slot available  → execute immediately
  All slots busy  → throw BulkheadRejectedException immediately

MaxQueueSize > 0  →  Queued bulkhead
  Slot available  → execute immediately (fast path via Wait(Zero))
  Slot busy, queue not full  → wait up to QueueTimeout for a slot
  Slot busy, queue full       → throw BulkheadRejectedException immediately
  Wait times out              → throw BulkheadRejectedException
```

### Isolation — two independent SemaphoreSlim instances

```csharp
var accountBulkhead = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 2 });
var networkBulkhead = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 2 });

// Saturate the account bulkhead (both slots held by slow calls)
// ...

accountBulkhead.Execute(() => accountSvc.GetAccount(id));  // ✗ rejected
networkBulkhead.Execute(() => networkSvc.GetStatus(region)); // ✓ succeeds — own semaphore
```

Each `BulkheadPolicy` owns its own `SemaphoreSlim`. Exhausting one has zero effect on any other.

### CountdownEvent + ManualResetEventSlim — deterministic concurrency in tests

```csharp
var holding = new CountdownEvent(2);   // counts down when 2 tasks are inside the bulkhead
var release = new ManualResetEventSlim(false);

var holders = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
    policy.Execute(() =>
    {
        holding.Signal();  // "I'm inside the bulkhead"
        release.Wait();    // hold the slot until the test tells me to release
        return 1;
    }))).ToArray();

holding.Wait();  // both slots are now deterministically occupied

Assert.Throws<BulkheadRejectedException>(() => policy.Execute(() => 1));

release.Set();   // let the holders complete
Task.WaitAll(holders);
```

`CountdownEvent` ensures we only proceed once both slots are confirmed occupied — no `Thread.Sleep` needed to "hope" the tasks have started.

## Demo Scenarios

```
=== Maple Connect — Bulkhead Pattern Demo ===

--- Normal Operation (3 concurrent calls, MaxConcurrency: 3) ---
  ✓ [1] ACC-1001 | Sarah Chen | Unlimited Plus
  ✓ [2] ACC-1002 | Sarah Chen | Unlimited Plus
  ✓ [3] ACC-1003 | Sarah Chen | Unlimited Plus
  [Available slots: 3/3]

--- Saturated (MaxConcurrency: 2, MaxQueueSize: 0 — excess calls rejected) ---
  [2 calls holding both execution slots...]
  ✗ [3] Bulkhead saturated — all 2 execution slot(s) are busy.
  ✗ [4] Bulkhead saturated — all 2 execution slot(s) are busy.
  [Available slots after release: 2/2]

--- Queue (MaxConcurrency: 2, MaxQueueSize: 2 — excess calls wait) ---
  ✓ [1] ACC-3001 — succeeded
  ✓ [2] ACC-3002 — succeeded
  ✓ [3] ACC-3003 — succeeded
  ✓ [4] ACC-3004 — succeeded
  (calls 3 and 4 queued and waited for slots to free up)

--- Isolation (Account bulkhead saturated — Network bulkhead unaffected) ---
  [Account Service bulkhead: both slots occupied]
  ✗ Account Service: Bulkhead saturated — all 2 execution slot(s) are busy.
  ✓ Network Service: Ontario | Operational | 142/145 towers
  Account service is isolated — the slow Account Service did not affect Network Service.
```

## When to Use

- Your application calls multiple downstream services and you need to prevent one slow dependency from taking down the others.
- Combined with Retry (4.17) and Circuit Breaker (4.16) to form a full resilience layer: Bulkhead caps concurrency, Retry handles transient failures, Circuit Breaker stops retries when failures are sustained.
- When you can characterise the expected concurrency per downstream service and want an explicit cap rather than relying on the default thread pool to absorb spikes.

## When NOT to Use

- Single-dependency applications — the added semaphore overhead has no isolation benefit with only one service.
- When all downstream services are equally critical and have the same performance profile — a shared limit may be simpler.
- Very high-throughput, low-latency paths where semaphore contention itself becomes a bottleneck — profile before adding.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Failure isolation | One saturated dependency cannot starve threads that belong to another. |
| Predictable resource usage | Each bulkhead has an explicit ceiling — thread use is bounded and visible. |
| Fast failure | `MaxQueueSize = 0` rejects immediately rather than queueing behind a slow service. |
| Observable state | `Available` and `Queued` are queryable — health endpoints and dashboards can expose bulkhead pressure. |
| Composable | Each `BulkheadPolicy` is an independent object; wrap any `Func<T>` regardless of the service type. |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Capacity tuning | `MaxConcurrency` and `MaxQueueSize` require per-service measurement and ongoing adjustment as load patterns change. |
| Over-rejection | Too-small a limit rejects valid calls under legitimate bursts; under-sized limits defeat the purpose. |
| Synchronous only | This implementation uses `SemaphoreSlim.Wait` — it blocks a thread while queued. An async variant would use `SemaphoreSlim.WaitAsync` to avoid holding threads during the wait. |

## Related Patterns

- **Retry Pattern (4.17)** — retries individual failed calls; Bulkhead caps how many can be in-flight simultaneously. Deploy together: Retry inside, Bulkhead outside.
- **Circuit Breaker (4.16)** — stops all calls when a service is persistently failing; Bulkhead limits concurrent calls to a service that is merely slow. Both are concurrency controls with different triggers.
- **Thread Pool Isolation** — a heavier variant: each downstream service gets its own dedicated thread pool rather than a semaphore count, so blocked threads are physically separated. `SemaphoreSlim` is the lightweight equivalent.
- **Rate Limiting / Throttle (4.29)** — limits calls *over time* (per second, per minute); Bulkhead limits calls *at a point in time* (simultaneous). They solve related but distinct problems.

## Running the Demo

```bash
cd src/4-Enterprise/4.18-Bulkhead/BulkheadPattern && dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.18-Bulkhead/BulkheadPattern.Tests && dotnet test
```
