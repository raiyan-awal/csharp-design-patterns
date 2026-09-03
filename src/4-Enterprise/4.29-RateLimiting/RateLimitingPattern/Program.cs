using RateLimitingPattern.Limiters;
using RateLimitingPattern.Middleware;

Console.WriteLine("=== 4.29 Rate Limiting / Throttle — Maple API Gateway ===\n");

// ─── 1. Fixed Window — public search endpoint ─────────────────────────────────
Console.WriteLine("─── 1. Fixed Window — /api/search (5 req / 10 s) ───");
var fixedLimiter = new FixedWindowRateLimiter(limit: 5, window: TimeSpan.FromSeconds(10));
var searchGateway = new ApiGateway(fixedLimiter, "/api/search");

for (var i = 1; i <= 8; i++)
{
    var ok = searchGateway.HandleRequest();
    Console.WriteLine($"  Request {i,2}: {(ok ? "✓ allowed" : "✗ rejected")}  " +
                      $"(available: {searchGateway.Available})");
}
Console.WriteLine($"\n  Handled: {searchGateway.RequestsHandled}  " +
                  $"Rejected: {searchGateway.RequestsRejected}");

Pause();

// ─── 2. Token Bucket — premium listings endpoint ─────────────────────────────
Console.WriteLine("─── 2. Token Bucket — /api/listings (capacity: 6, refill: 2/s) ───");

// FakeClock for the demo so we can advance time in the console
var t = DateTimeOffset.UtcNow;
var tokenLimiter = new TokenBucketRateLimiter(capacity: 6, refillRatePerSecond: 2,
                                               clock: () => t);
var listingsGateway = new ApiGateway(tokenLimiter, "/api/listings");

Console.WriteLine("  Phase A — burst (drain the bucket):");
for (var i = 1; i <= 8; i++)
{
    var ok = listingsGateway.HandleRequest();
    Console.WriteLine($"    Request {i}: {(ok ? "✓" : "✗")}  tokens remaining: {listingsGateway.Available}");
}

Console.WriteLine("\n  Phase B — wait 2 seconds (4 tokens refill), then send 5 more:");
t = t.AddSeconds(2);
for (var i = 1; i <= 5; i++)
{
    var ok = listingsGateway.HandleRequest();
    Console.WriteLine($"    Request {i}: {(ok ? "✓" : "✗")}  tokens remaining: {listingsGateway.Available}");
}

Console.WriteLine($"\n  Total handled: {listingsGateway.RequestsHandled}  " +
                  $"Total rejected: {listingsGateway.RequestsRejected}");

Pause();

// ─── 3. Sliding Window — eliminates the boundary burst ───────────────────────
Console.WriteLine("─── 3. Sliding Window — /api/analytics (5 req / 10 s, rolling) ───");
var sw_t = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
var slidingLimiter = new SlidingWindowRateLimiter(limit: 5, window: TimeSpan.FromSeconds(10),
                                                   clock: () => sw_t);
var analyticsGateway = new ApiGateway(slidingLimiter, "/api/analytics");

Console.WriteLine("  Phase A — fill the window at t=9 s:");
sw_t = sw_t.AddSeconds(9);
for (var i = 1; i <= 5; i++)
{
    var ok = analyticsGateway.HandleRequest();
    Console.WriteLine($"    t= 9 s  Request {i}: {(ok ? "✓" : "✗")}  available: {analyticsGateway.Available}");
}

Console.WriteLine("\n  Phase B — t=10 s: Fixed Window would reset here; Sliding Window does not:");
sw_t = sw_t.AddSeconds(1);   // t=10 s; requests at t=9 are only 1 s old — still in window
var blocked = analyticsGateway.HandleRequest();
Console.WriteLine($"    t=10 s  Request: {(blocked ? "✓ allowed" : "✗ rejected — 5 requests at t=9 still inside rolling window")}");

Console.WriteLine("\n  Phase C — t=19 s: requests from t=9 are 10 s old and evicted:");
sw_t = sw_t.AddSeconds(9);   // t=19 s; cutoff = 9 s; all t=9 timestamps evicted
for (var i = 1; i <= 5; i++)
{
    var ok = analyticsGateway.HandleRequest();
    Console.WriteLine($"    t=19 s  Request {i}: {(ok ? "✓" : "✗")}  available: {analyticsGateway.Available}");
}

Pause();

// ─── 4. Two independent gateways — no cross-contamination ────────────────────
Console.WriteLine("─── 4. Two independent endpoints, separate limits ───");
var userLimiter = new FixedWindowRateLimiter(limit: 3, window: TimeSpan.FromSeconds(60));
var orderLimiter = new FixedWindowRateLimiter(limit: 2, window: TimeSpan.FromSeconds(60));
var userGateway  = new ApiGateway(userLimiter,  "/api/users");
var orderGateway = new ApiGateway(orderLimiter, "/api/orders");

Console.WriteLine($"  /api/users  (limit 3): " +
    string.Join(" ", Enumerable.Range(1, 4).Select(_ => userGateway.HandleRequest() ? "✓" : "✗")));
Console.WriteLine($"  /api/orders (limit 2): " +
    string.Join(" ", Enumerable.Range(1, 4).Select(_ => orderGateway.HandleRequest() ? "✓" : "✗")));
Console.WriteLine("  Limits are enforced independently — one endpoint's traffic does not affect the other.");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
