# 4.29 — Rate Limiting / Throttle

## Intent

Rate Limiting controls how many requests a caller can make within a given time period. The limiter sits in front of a service, counts incoming requests, and rejects any that exceed the configured threshold. It protects services from overload, abuse, and runaway clients while guaranteeing fair access for all callers.

## The Problem It Solves

Without rate limiting, a single caller can monopolize a service's capacity:

```csharp
// Without rate limiting: every request is processed unconditionally
public SearchResult Search(string query)
{
    // a single client can call this 10,000 times/second with no consequence
    return _searchEngine.Execute(query);
}
```

Problems this creates:
- **Overload** — a misbehaving or accidentally looping client exhausts CPU, database connections, or downstream API quotas for everyone.
- **Cascading failures** — an overloaded service slows down, queue depths grow, and upstream services start timing out too.
- **Cost runaway** — cloud APIs charge per call; an unthrottled client can generate unexpected bills overnight.
- **No fairness** — one greedy caller can crowd out all other clients competing for the same resource.

## Solution: Three Algorithms Behind One Interface

All three algorithms implement `IRateLimiter` with a single `TryAcquire()` method. The gateway calls `TryAcquire()` before every request; the algorithm decides whether to allow or reject.

```csharp
public interface IRateLimiter
{
    bool TryAcquire();   // true = proceed; false = reject
    int Available { get; }
    int Limit { get; }
    string Algorithm { get; }
}

// Gateway is algorithm-agnostic
var gateway = new ApiGateway(new FixedWindowRateLimiter(10, TimeSpan.FromMinutes(1)),
                              "/api/search");
bool allowed = gateway.HandleRequest();   // ask the limiter, track stats
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Rate limiter interface | `IRateLimiter` | `TryAcquire()` + `Available`, `Limit`, `Algorithm` |
| Fixed Window limiter | `FixedWindowRateLimiter` | Counts requests in a fixed time window; resets when window expires |
| Token Bucket limiter | `TokenBucketRateLimiter` | Maintains a token pool; tokens refill at a constant rate |
| Sliding Window limiter | `SlidingWindowRateLimiter` | Stores a timestamp queue; evicts old entries on each call; eliminates boundary burst |
| Gateway | `ApiGateway` | Wraps a limiter around a named endpoint; tracks handled/rejected counts |
| Exception | `RateLimitExceededException` | Thrown by callers that prefer exceptions over `false` return values |

## Structure

```
4.29-RateLimiting/
├── RateLimitingPattern/
│   ├── Core/
│   │   ├── IRateLimiter.cs               ← TryAcquire + Available / Limit / Algorithm
│   │   └── RateLimitExceededException.cs ← exception for callers that prefer throw
│   ├── Limiters/
│   │   ├── FixedWindowRateLimiter.cs     ← counter + window expiry; injectable clock
│   │   ├── TokenBucketRateLimiter.cs     ← token pool; refill on elapsed time; injectable clock
│   │   └── SlidingWindowRateLimiter.cs   ← timestamp queue; evict on each call; no boundary burst
│   ├── Middleware/
│   │   └── ApiGateway.cs                 ← per-endpoint wrapper; RequestsHandled + RequestsRejected
│   └── Program.cs
└── RateLimitingPattern.Tests/
    └── RateLimitingPatternTests.cs        ← 35 tests across 5 suites; FakeClock for deterministic time
```

## Key Code

### Fixed Window Counter

```csharp
public bool TryAcquire()
{
    RefreshIfExpired();          // reset counter when window ends
    if (_count >= _limit) return false;
    _count++;
    return true;
}

private void RefreshIfExpired()
{
    var now = _clock();
    if (_windowStart == DateTimeOffset.MinValue || now >= _windowStart + _window)
    {
        _windowStart = now;
        _count = 0;
    }
}
```

Simple and predictable. Weakness: a burst at the end of one window and the start of the next can briefly allow up to 2× the configured limit — the "boundary burst" problem. See **Sliding Window** below for a fix.

### Token Bucket

```csharp
public bool TryAcquire()
{
    Refill();
    if (_tokens < 1) return false;
    _tokens -= 1;
    return true;
}

private void Refill()
{
    var elapsed = (now - _lastRefill).TotalSeconds;
    _tokens = Math.Min(_capacity, _tokens + elapsed * _refillRatePerSecond);
    _lastRefill = now;
}
```

Tokens accumulate while traffic is low, enabling a controlled burst later. The bucket cap prevents unlimited accumulation. This models the common "allow spikes but enforce a sustained rate" requirement.

### Sliding Window — no boundary burst

```csharp
public bool TryAcquire()
{
    var now = _clock();
    Evict(now);
    if (_timestamps.Count >= _limit) return false;
    _timestamps.Enqueue(now);
    return true;
}

private void Evict(DateTimeOffset now)
{
    var cutoff = now - _window;
    while (_timestamps.Count > 0 && _timestamps.Peek() <= cutoff)
        _timestamps.Dequeue();
}
```

Every call first evicts timestamps older than the window, then checks how many remain. A request at the very start of a new "minute" is still blocked if requests from the last few seconds of the previous minute fill the queue. This eliminates the boundary burst entirely. The trade-off: storing up to `limit` timestamps per limiter instance (O(limit) memory) rather than a single counter.

### Injectable clock — deterministic tests

Both limiters accept `Func<DateTimeOffset>? clock = null`, defaulting to `DateTimeOffset.UtcNow`. Tests inject a `FakeClock` whose `Advance(TimeSpan)` moves time forward without sleeping:

```csharp
var clock   = new FakeClock(DateTimeOffset.UtcNow);
var limiter = new TokenBucketRateLimiter(capacity: 6, refillRatePerSecond: 2, clock: clock.Now);

for (var i = 0; i < 6; i++) limiter.TryAcquire();   // drain bucket
Assert.False(limiter.TryAcquire());

clock.Advance(TimeSpan.FromSeconds(1));              // +2 tokens — no real sleep
Assert.True(limiter.TryAcquire());
```

### ApiGateway — algorithm-agnostic

```csharp
public bool HandleRequest()
{
    if (!limiter.TryAcquire())
    {
        RequestsRejected++;
        return false;
    }
    RequestsHandled++;
    return true;
}
```

The gateway does not know which algorithm its limiter uses. Swap `FixedWindowRateLimiter` for `TokenBucketRateLimiter` or `SlidingWindowRateLimiter` without changing the gateway or its callers.

## Demo Scenarios

```
1. Fixed Window — /api/search (5 req / 10 s)        — burst of 8; 5 allowed, 3 rejected
2. Token Bucket — /api/listings (capacity 6, 2/s)   — drain bucket, wait 2 s, refill 4 tokens, continue
3. Sliding Window — /api/analytics (5 req / 10 s)   — fill window at t=9 s; boundary at t=10 s still blocked; clear at t=19 s
4. Two independent endpoints                         — separate limiters; one endpoint's traffic does not affect the other
```

## Algorithm Comparison

| Property | Fixed Window | Token Bucket | Sliding Window |
|----------|-------------|--------------|----------------|
| Burst handling | Hard cutoff at window boundary | Allows bursts up to bucket capacity | Hard cutoff; always rolling |
| Boundary burst risk | Yes — 2× limit across window edge | No — tokens cap the burst | No — window is always rolling |
| Memory | O(1) — one counter | O(1) — one token value | O(limit) — one timestamp per request |
| Implementation | Simple counter + timestamp | Running token count + elapsed refill | Queue of timestamps; evict on each call |
| Use when | Simple per-minute/hour quotas | APIs that need to absorb spikes gracefully | Strict per-window enforcement with no boundary leakage |

## When to Use

- Protecting public or partner APIs from accidental or intentional overuse.
- Enforcing fair-use policies: each tenant or API key gets a share of capacity.
- Preventing runaway retry loops from cascading into downstream failures.
- Cloud API cost control: limit calls to third-party services that charge per request.

## When NOT to Use

- Internal service-to-service calls on a trusted network where overload is handled by upstream load shedding instead.
- Batch processing jobs that legitimately need sustained high throughput — a rate limiter will throttle them below their required rate.
- When coordination across multiple server instances is required — in-process limiters (like these) do not share state; production needs a shared store (Redis).

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Overload protection | The service degrades gracefully — excess requests are rejected cleanly rather than queuing indefinitely |
| Fair access | No single caller can crowd out others |
| Pluggable algorithms | `IRateLimiter` lets you swap Fixed Window for Token Bucket for Sliding Window without touching callers |
| Testability | Injectable clock means every time-dependent scenario is tested deterministically |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| In-process only | These limiters live in one process; distributed deployments need a shared counter (Redis INCR, etc.) |
| Fixed Window boundary burst | A caller can send limit requests at the end of one window and limit more at the start of the next, briefly doubling effective throughput |
| Rejection is lossy | Rejected requests are dropped — callers must implement retry with backoff to handle 429 responses correctly |

## Related Patterns

- **Circuit Breaker (4.16)** — stops calls to a failing downstream service; Rate Limiting stops calls from an abusive upstream caller. Often combined at the gateway layer.
- **Bulkhead (4.18)** — isolates resources with concurrency limits; Rate Limiting constrains throughput over time. They complement each other.
- **Retry Pattern (4.17)** — clients that receive a 429 / false from a rate limiter should retry with exponential backoff to avoid thundering-herd re-entry.

## Running the Demo

```bash
cd src/4-Enterprise/4.29-RateLimiting/RateLimitingPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.29-RateLimiting/RateLimitingPattern.Tests
dotnet test
```
