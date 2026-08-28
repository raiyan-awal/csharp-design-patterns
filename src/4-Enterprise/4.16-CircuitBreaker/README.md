# 4.16 — Circuit Breaker

## Intent

The Circuit Breaker prevents cascading failures by detecting when a downstream service is unhealthy and stopping calls to it immediately — instead of letting every request hang until timeout. Once tripped, it gives the failing service time to recover before allowing traffic through again.

## The Problem It Solves

```csharp
// Without Circuit Breaker: every call waits for timeout when the service is down
for (var i = 0; i < 100; i++)
{
    try
    {
        var rate = canadaPostApi.GetRate(origin, destination, weight);
        // Each call hangs for 30 seconds before timing out
    }
    catch (TimeoutException)
    {
        // 100 calls × 30s timeout = 50 minutes of blocked threads
    }
}
```

Problems:

- **Thread starvation.** Every in-flight call occupies a thread while waiting for a timeout. Under load, the thread pool exhausts and the entire application becomes unresponsive.
- **Cascading failures.** A slow downstream service causes queues to fill, memory to grow, and upstream callers to time out too — one failing dependency brings down a healthy service.
- **No recovery window.** The failing service gets hammered with retries at full traffic volume exactly when it is least able to handle them, preventing recovery.
- **No fast feedback.** Callers wait the full timeout duration before finding out the service is down, even when it has been down for minutes.

## Solution: Three-State Circuit Breaker

```csharp
var cb = new CircuitBreaker(new CircuitBreakerOptions
{
    FailureThreshold = 3,               // open after 3 consecutive failures
    SuccessThreshold = 2,               // close after 2 successes in Half-Open
    ResetTimeout     = TimeSpan.FromSeconds(30)   // wait 30s before probing
});

try
{
    var rate = cb.Execute(() => shippingApi.GetRate(origin, destination, weight));
}
catch (CircuitBreakerOpenException)
{
    // Fast failure — no thread held, no network call made
    return FallbackRate();
}
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Circuit Breaker | `CircuitBreaker` | Tracks state, failure/success counts, and timeout; wraps any `Func<T>` |
| Options | `CircuitBreakerOptions` | `FailureThreshold`, `SuccessThreshold`, `ResetTimeout` |
| State | `CircuitState` | `Closed` / `Open` / `HalfOpen` enum |
| Exception | `CircuitBreakerOpenException` | Thrown when a call is rejected because the circuit is Open |
| Service | `IShippingRateService` | The downstream dependency being protected |
| Fake | `SimulatedShippingRateService` | Controllable implementation — switch between healthy and failing |

## Structure

```
4.16-CircuitBreaker/
├── CircuitBreakerPattern/
│   ├── Core/
│   │   ├── CircuitState.cs               ← Closed / Open / HalfOpen
│   │   ├── CircuitBreakerOptions.cs      ← thresholds + timeout
│   │   ├── CircuitBreakerOpenException.cs
│   │   └── CircuitBreaker.cs             ← state machine; Execute<T>
│   ├── Services/
│   │   ├── ShippingRate.cs               ← result record
│   │   ├── IShippingRateService.cs
│   │   └── SimulatedShippingRateService.cs ← SetHealthy() / SetFailing()
│   └── Program.cs                        ← 4-section demo
└── CircuitBreakerPattern.Tests/
    └── CircuitBreakerTests.cs            ← 18 tests across 4 suites
```

## Key Code

### Three-state transition logic

```
┌─────────────────────────────────────────────────────────────┐
│                         CLOSED                              │
│  Calls pass through. Failure count tracked.                 │
│  failures >= threshold ──────────────────────────► OPEN     │
└─────────────────────────────────────────────────────────────┘
         ▲                                            │
         │ successes >= successThreshold              │ reset timeout elapsed
         │                                            ▼
┌─────────────────────────────────────────────────────────────┐
│                        HALF-OPEN                            │
│  Trial state. Calls let through to probe recovery.          │
│  Any failure ──────────────────────────────────────► OPEN   │
└─────────────────────────────────────────────────────────────┘
```

### CircuitBreaker.Execute — the central method

```csharp
public T Execute<T>(Func<T> action)
{
    lock (_lock)
    {
        if (_state == CircuitState.Open)
        {
            if (_utcNow() - _openedAt!.Value >= _options.ResetTimeout)
                TransitionTo(CircuitState.HalfOpen);
            else
                throw new CircuitBreakerOpenException("Circuit is Open — service unavailable.");
        }
    }

    try
    {
        var result = action();
        lock (_lock) { OnSuccess(); }
        return result;
    }
    catch (Exception)
    {
        lock (_lock) { OnFailure(); }
        throw;
    }
}
```

State is checked under a lock, then the action runs outside the lock so slow calls do not block other threads from checking state. Success and failure are recorded under a separate lock acquisition after the action completes.

### Injected clock — deterministic testing without Thread.Sleep

```csharp
public CircuitBreaker(CircuitBreakerOptions options, Func<DateTime>? utcNow = null)
{
    _options = options;
    _utcNow  = utcNow ?? (() => DateTime.UtcNow);
}
```

`Func<DateTime>` is injected so tests can advance time instantly. Without it, a test that needs to verify Half-Open behaviour would require a real `Thread.Sleep(30_000)`. With it:

```csharp
var now = DateTime.UtcNow;
var cb  = new CircuitBreaker(options, () => now);

Fail(cb, 3);            // trips to Open
now = now.AddSeconds(31);  // advance clock past reset timeout

cb.Execute(() => 1);    // transitions to HalfOpen immediately
```

### OnFailure and OnSuccess — state transitions

```csharp
private void OnFailure()
{
    _failureCount++;
    if (_state == CircuitState.HalfOpen || _failureCount >= _options.FailureThreshold)
        TransitionTo(CircuitState.Open);
}

private void OnSuccess()
{
    _failureCount = 0;
    if (_state == CircuitState.HalfOpen)
    {
        _successCount++;
        if (_successCount >= _options.SuccessThreshold)
            TransitionTo(CircuitState.Closed);
    }
}
```

A single failure in Half-Open immediately re-opens — the service has not recovered. Multiple successes in Half-Open are required before trusting the service again, preventing a flapping circuit that opens and closes on every retry.

## Demo Scenarios

```
=== Maple Commerce — Circuit Breaker Demo ===

--- Normal Operation (Circuit: Closed) ---
  ✓ Toronto, ON → Vancouver, BC | $17.99 CAD | 5 days
  ✓ Montreal, QC → Calgary, AB  | $16.99 CAD | 5 days
  ✓ Ottawa, ON → Halifax, NS    | $14.99 CAD | 3 days
  [Circuit: Closed   | Failures: 0 | Successes: 0]

--- Service Degradation (Canada Post API failing) ---
  ✗ [SERVICE ERROR] Canada Post Rate API is currently unavailable (503 Service Unavailable).
  [Circuit: Closed   | Failures: 1 | Successes: 0]
  ✗ [SERVICE ERROR] Canada Post Rate API is currently unavailable (503 Service Unavailable).
  [Circuit: Closed   | Failures: 2 | Successes: 0]
  ✗ [SERVICE ERROR] Canada Post Rate API is currently unavailable (503 Service Unavailable).
  [Circuit: Open     | Failures: 3 | Successes: 0]

--- Open Circuit (rejecting immediately, service not called) ---
  ✗ [CIRCUIT OPEN] Circuit is Open — service unavailable. Retry after 3s.
  ✗ [CIRCUIT OPEN] Circuit is Open — service unavailable. Retry after 3s.
  Service call count before: 6 | after: 6
  Calls blocked by circuit breaker: 0

--- Half-Open: Failure Re-Opens Circuit ---
  Service is still failing. Waiting 3s for reset timeout to expire...
  [Timeout elapsed — next call probes the service (Half-Open)]

  ✗ [SERVICE ERROR] Canada Post Rate API is currently unavailable (503 Service Unavailable).
  [Circuit: Open     | Failures: 1 | Successes: 0]

  A single failure in Half-Open immediately re-opens the circuit.
  The service gets no partial credit — it must prove it has recovered.

--- Recovery (waiting for reset timeout, then Half-Open → Closed) ---
  Waiting 3s for reset timeout to expire...
  [Canada Post API restored]

  ✓ Toronto, ON → Vancouver, BC | $17.49 CAD | 5 days
  [Circuit: HalfOpen | Failures: 0 | Successes: 1]
  ✓ Montreal, QC → Calgary, AB  | $16.24 CAD | 5 days
  [Circuit: Closed   | Failures: 0 | Successes: 0]
  Circuit recovered — normal operation resumed:
  ✓ Ottawa, ON → Halifax, NS    | $14.99 CAD | 3 days
  [Circuit: Closed   | Failures: 0 | Successes: 0]
```

## When to Use

- Your service calls a downstream HTTP API, database, or external system that can become slow or unavailable.
- You want fast failure instead of threads blocked on timeouts accumulating under load.
- You need to give a failing dependency breathing room to recover rather than hammering it with retries.
- Combined with a fallback (cached result, default value, degraded response) to maintain partial availability when the circuit is Open.

## When NOT to Use

- Simple single-process applications with no external dependencies — the overhead is unnecessary.
- Calls that are idempotent and safe to retry endlessly — a simple retry policy may be sufficient.
- Dependencies that fail only because of bad input, not because of service health — opening the circuit on a `400 Bad Request` would be incorrect.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Fast failure | Open circuit rejects immediately — no thread blocked, no network socket held. |
| Cascading failure prevention | Stops the failure from propagating to upstream callers by eliminating wait time. |
| Recovery window | The failing service gets time at reduced traffic to stabilise before full load returns. |
| Observability | `State`, `FailureCount`, and `SuccessCount` are queryable — dashboards and health endpoints can expose circuit state. |
| Testable | Clock injection makes state transitions fully deterministic without `Thread.Sleep`. |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Stale open state | If the reset timeout is too long, the circuit stays open long after the service recovers, rejecting valid calls. |
| False positives | A brief network blip can trip the circuit and cause unnecessary rejections; tuning thresholds for each dependency takes time. |
| No fallback included | The circuit breaker only prevents calls — providing a cached or degraded fallback is the caller's responsibility. |

## Related Patterns

- **Retry Pattern (4.17)** — Retry handles transient failures with back-off; Circuit Breaker handles sustained failures by stopping retries entirely. They complement each other: retry inside a closed circuit, fail fast when open.
- **Bulkhead (4.18)** — isolates thread pools per dependency so one slow service cannot exhaust resources; Circuit Breaker and Bulkhead are often deployed together.
- **Health Endpoint Monitoring (4.30)** — exposes circuit breaker state (`Closed` / `Open` / `HalfOpen`) via a health endpoint so orchestrators can route traffic away from an unhealthy instance.
- **Proxy (2.7)** — the circuit breaker is structurally a proxy: it wraps the real service and intercepts calls. The difference is intent — Proxy adds access control or caching; Circuit Breaker adds resilience.

## Running the Demo

```bash
cd src/4-Enterprise/4.16-CircuitBreaker/CircuitBreakerPattern && dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.16-CircuitBreaker/CircuitBreakerPattern.Tests && dotnet test
```
