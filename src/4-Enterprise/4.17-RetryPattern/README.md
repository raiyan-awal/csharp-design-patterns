# 4.17 — Retry Pattern

## Intent

The Retry Pattern handles transient failures by automatically re-attempting a failed operation — with a configurable number of attempts, a delay strategy between attempts, and a predicate that decides which exceptions are worth retrying. It separates retry logic from business logic so neither has to know about the other.

## The Problem It Solves

```csharp
// Without Retry: caller handles every transient failure manually
PaymentResult? result = null;
for (var i = 0; i < 3; i++)
{
    try
    {
        result = gateway.ProcessPayment(token, amount, orderId);
        break;
    }
    catch (HttpRequestException)
    {
        if (i == 2) throw;
        Thread.Sleep(1000);
    }
}
```

Problems:

- **Retry logic is duplicated.** Every service call that needs resilience gets its own loop, sleep, and exception filter — all written by hand, all slightly different.
- **No strategy variation.** Changing from fixed to exponential back-off means touching every retry loop in the codebase.
- **No observability hook.** Logging a warning on retry requires adding it to every loop individually.
- **Business logic is obscured.** The retry scaffolding wraps the actual intent — `ProcessPayment` — making the code harder to read and test.

## Solution: Configurable RetryPolicy

```csharp
var policy = new RetryPolicy(new RetryOptions
{
    MaxAttempts   = 5,
    DelayStrategy = DelayStrategy.Exponential(TimeSpan.FromMilliseconds(100)),
    ShouldRetry   = ex => ex is HttpRequestException,
    OnRetry       = (ex, attempt, delay) =>
        Console.WriteLine($"Attempt {attempt} failed — retrying in {delay.TotalMilliseconds}ms")
});

var result = policy.Execute(() => gateway.ProcessPayment(token, amount, orderId));
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Policy | `RetryPolicy` | Executes the action; catches retryable exceptions; sleeps between attempts |
| Options | `RetryOptions` | `MaxAttempts`, `DelayStrategy`, `ShouldRetry`, `OnRetry` |
| Delay | `DelayStrategy` | Static factory: `Fixed`, `Exponential`, `ExponentialWithJitter` |
| Exception | `RetryExhaustedException` | Thrown when all attempts are exhausted; wraps the last exception |
| Service | `IPaymentGateway` | The downstream dependency being called |
| Fake | `SimulatedPaymentGateway` | Controllable fake — `FailTimes(n)`, `Decline()`, `SetHealthy()` |

## Structure

```
4.17-RetryPattern/
├── RetryPattern/
│   ├── Core/
│   │   ├── RetryExhaustedException.cs    ← wraps last exception; exposes Attempts
│   │   ├── DelayStrategy.cs              ← Fixed / Exponential / ExponentialWithJitter
│   │   ├── RetryOptions.cs               ← MaxAttempts, DelayStrategy, ShouldRetry, OnRetry
│   │   └── RetryPolicy.cs                ← Execute<T>; the retry loop
│   ├── Services/
│   │   ├── PaymentResult.cs              ← result record
│   │   ├── PaymentDeclinedException.cs   ← non-retryable exception
│   │   ├── IPaymentGateway.cs
│   │   └── SimulatedPaymentGateway.cs    ← FailTimes / Decline / SetHealthy
│   └── Program.cs                        ← 4-section demo
└── RetryPattern.Tests/
    └── RetryPolicyTests.cs               ← 20 tests across 5 suites
```

## Key Code

### RetryPolicy.Execute — the retry loop

```csharp
public T Execute<T>(Func<T> action)
{
    Exception? lastException = null;

    for (var attempt = 1; attempt <= _options.MaxAttempts; attempt++)
    {
        try
        {
            return action();
        }
        catch (Exception ex) when (_options.ShouldRetry(ex))
        {
            lastException = ex;

            if (attempt == _options.MaxAttempts)
                break;

            var delay = _options.DelayStrategy(attempt);
            _options.OnRetry?.Invoke(ex, attempt, delay);
            _sleep(delay);
        }
        // Non-retryable exceptions propagate naturally — the when clause evaluates false
    }

    throw new RetryExhaustedException(
        $"Operation failed after {_options.MaxAttempts} attempt(s).",
        _options.MaxAttempts,
        lastException!);
}
```

The `when (_options.ShouldRetry(ex))` clause is the key decision point. When it returns `false`, C# does not enter the `catch` block — the exception propagates immediately, unwrapped, with its original stack trace intact. This means a `PaymentDeclinedException` on attempt 1 reaches the caller as-is, without any retry delay and without being wrapped in `RetryExhaustedException`.

### Non-retryable exception propagation

```csharp
ShouldRetry = ex => ex is HttpRequestException  // transient — retry
                                                 // PaymentDeclinedException → when = false → propagates as-is
```

This distinction matters: a network timeout is transient and worth retrying; a card decline is permanent — retrying would just produce the same result and delay the error response to the customer.

### Three delay strategies

```csharp
// Attempt 1, 2, 3: 500ms, 500ms, 500ms
DelayStrategy.Fixed(TimeSpan.FromMilliseconds(500))

// Attempt 1, 2, 3: 100ms, 200ms, 400ms (doubles each time)
DelayStrategy.Exponential(TimeSpan.FromMilliseconds(100))

// Attempt 1, 2, 3: ~100ms ±25%, ~200ms ±25%, ~400ms ±25%
// Jitter prevents a thundering herd when many clients retry simultaneously
DelayStrategy.ExponentialWithJitter(TimeSpan.FromMilliseconds(100))
```

`ExponentialWithJitter` adds ±25% randomness to each delay. Without jitter, every client that hit the same failure at the same time would retry at the same instant, creating a second spike — the thundering herd. Spreading retries randomly over a window reduces that spike.

### Injectable sleep — deterministic tests without Thread.Sleep

```csharp
public RetryPolicy(RetryOptions options, Action<TimeSpan>? sleep = null)
{
    _options = options;
    _sleep   = sleep ?? (ts => Thread.Sleep(ts));
}
```

Tests pass `sleep: _ => { }` to skip all delays. The demo uses the default (`Thread.Sleep`) so real time passes between retries as a human would observe. The same injectable parameter pattern as the Circuit Breaker's `Func<DateTime>` clock.

## Demo Scenarios

```
=== Maple Pay — Retry Pattern Demo ===

--- Fixed Delay Retry (2 transient failures, then success) ---
  [Attempt 1 failed: Payment gateway timeout (503 Service Unavailable).] Retrying in 200ms...
  [Attempt 2 failed: Payment gateway timeout (503 Service Unavailable).] Retrying in 200ms...

  ✓ Payment approved: TXN-A3F2... | $149.99 CAD

--- Exponential Back-off (delays double with each attempt) ---
  [Attempt 1 failed: Payment gateway timeout (503 Service Unavailable).] Retrying in 100ms...
  [Attempt 2 failed: Payment gateway timeout (503 Service Unavailable).] Retrying in 200ms...
  [Attempt 3 failed: Payment gateway timeout (503 Service Unavailable).] Retrying in 400ms...

  ✓ Payment approved: TXN-B7C1... | $89.00 CAD

--- Non-Retryable Exception (card declined — retried zero times) ---
  ✗ Card declined: insufficient funds.
  Gateway calls before: 6 | after: 7
  Calls attempted: 1 (no retries — failed immediately)

--- Retries Exhausted (all 3 attempts fail) ---
  [Attempt 1 failed: Payment gateway timeout (503 Service Unavailable).]  Retrying in 100ms...
  [Attempt 2 failed: Payment gateway timeout (503 Service Unavailable).]  Retrying in 100ms...

  ✗ [RETRIES EXHAUSTED] Operation failed after 3 attempt(s).
  Root cause: Payment gateway timeout (503 Service Unavailable).
```

## When to Use

- Calling remote services (HTTP APIs, databases, message brokers) that experience transient failures — timeouts, brief unavailability, throttling.
- Combined with Circuit Breaker: retry inside a closed circuit; stop retrying when the circuit opens.
- When the operation is idempotent — the same call can safely be made multiple times without side effects (read-only queries, payments with an idempotency key).

## When NOT to Use

- Non-idempotent operations without an idempotency key — retrying `CreateOrder` could create duplicate orders.
- Failures caused by bad input (`400 Bad Request`, `PaymentDeclinedException`) — retrying produces the same error immediately and delays the response.
- When the downstream service is known to be down for an extended period — use Circuit Breaker instead so retries stop entirely.
- Indefinite retrying without a `MaxAttempts` cap — it can block threads and mask systemic failures.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Decoupled resilience | Retry logic lives in the policy, not scattered through every service call. |
| Configurable strategy | Swap `Fixed` for `Exponential` or `ExponentialWithJitter` at the composition root with no code change. |
| Selective retry | `ShouldRetry` predicate decides per-exception-type — transient errors retry, permanent errors propagate immediately. |
| Observability | `OnRetry` callback fires before each delay — wire it to a logger or metrics counter without changing the core logic. |
| Testable | Injectable `sleep` action makes retry delays skippable in tests — no `Thread.Sleep` in the test suite. |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Amplified load | Each retry is an additional call to the already-struggling downstream service; exponential back-off and a circuit breaker mitigate this. |
| Increased latency | Even a single retry with exponential back-off adds visible latency; callers must not assume low response time on failure paths. |
| Duplicate side effects | Without idempotency, retrying a write operation can create duplicates — the Outbox Pattern (4.20) solves this for message publishing. |

## Related Patterns

- **Circuit Breaker (4.16)** — the natural pair: Retry handles individual transient failures; Circuit Breaker stops retrying entirely when a service is persistently down. Deploy them together.
- **Outbox Pattern (4.20)** — makes retried message publishing idempotent by recording the outgoing message before sending, preventing duplicate events on retry.
- **Bulkhead (4.18)** — isolates retry thread pools per dependency so a high-retry-rate downstream cannot exhaust shared thread resources.
- **Result Pattern (4.21)** — `RetryExhaustedException` vs. a `Result<T>` return type: some teams prefer the policy to return `Result.Fail(lastException)` on exhaustion rather than throw, keeping all error paths as return values.

## Running the Demo

```bash
cd src/4-Enterprise/4.17-RetryPattern/RetryPattern && dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.17-RetryPattern/RetryPattern.Tests && dotnet test
```
