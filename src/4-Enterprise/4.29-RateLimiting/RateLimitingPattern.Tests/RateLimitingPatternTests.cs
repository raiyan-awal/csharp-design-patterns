using RateLimitingPattern.Core;
using RateLimitingPattern.Limiters;
using RateLimitingPattern.Middleware;
using Xunit;

namespace RateLimitingPattern.Tests;

// ─── FakeClock ────────────────────────────────────────────────────────────────

sealed class FakeClock(DateTimeOffset initial)
{
    private DateTimeOffset _now = initial;
    public DateTimeOffset Now() => _now;
    public void Advance(TimeSpan by) => _now += by;
}

// ─── FixedWindowRateLimiter ───────────────────────────────────────────────────

public class FixedWindowRateLimiter_Tests
{
    private static (FixedWindowRateLimiter Limiter, FakeClock Clock) Build(
        int limit = 3, int windowSeconds = 10)
    {
        var clock   = new FakeClock(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        var limiter = new FixedWindowRateLimiter(limit, TimeSpan.FromSeconds(windowSeconds),
                                                 clock.Now);
        return (limiter, clock);
    }

    [Fact]
    public void AllowsRequests_UpToTheLimit()
    {
        var (limiter, _) = Build(limit: 3);
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
    }

    [Fact]
    public void RejectsRequest_WhenLimitReached()
    {
        var (limiter, _) = Build(limit: 2);
        limiter.TryAcquire();
        limiter.TryAcquire();
        Assert.False(limiter.TryAcquire());
    }

    [Fact]
    public void Available_DecrementsWithEachRequest()
    {
        var (limiter, _) = Build(limit: 5);
        Assert.Equal(5, limiter.Available);
        limiter.TryAcquire();
        Assert.Equal(4, limiter.Available);
        limiter.TryAcquire();
        Assert.Equal(3, limiter.Available);
    }

    [Fact]
    public void Available_IsZero_WhenLimitReached()
    {
        var (limiter, _) = Build(limit: 2);
        limiter.TryAcquire();
        limiter.TryAcquire();
        Assert.Equal(0, limiter.Available);
    }

    [Fact]
    public void WindowReset_AllowsNewRequests_AfterWindowExpires()
    {
        var (limiter, clock) = Build(limit: 2, windowSeconds: 10);
        limiter.TryAcquire();
        limiter.TryAcquire();
        Assert.False(limiter.TryAcquire());   // exhausted

        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.True(limiter.TryAcquire());    // new window — allowed
    }

    [Fact]
    public void WindowDoesNotReset_BeforeWindowExpires()
    {
        var (limiter, clock) = Build(limit: 2, windowSeconds: 10);
        limiter.TryAcquire();
        limiter.TryAcquire();

        clock.Advance(TimeSpan.FromSeconds(9));
        Assert.False(limiter.TryAcquire());   // window still active
    }

    [Fact]
    public void MultipleWindows_Each_ResetsCorrectly()
    {
        var (limiter, clock) = Build(limit: 1, windowSeconds: 10);
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());

        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());

        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.True(limiter.TryAcquire());
    }

    [Fact]
    public void LimitProperty_ReturnsConfiguredLimit()
    {
        var (limiter, _) = Build(limit: 7);
        Assert.Equal(7, limiter.Limit);
    }

    [Fact]
    public void Algorithm_IsFixedWindow()
    {
        var (limiter, _) = Build();
        Assert.Equal("Fixed Window", limiter.Algorithm);
    }
}

// ─── TokenBucketRateLimiter ───────────────────────────────────────────────────

public class TokenBucketRateLimiter_Tests
{
    private static (TokenBucketRateLimiter Limiter, FakeClock Clock) Build(
        int capacity = 5, double refillRate = 2.0)
    {
        var clock   = new FakeClock(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        var limiter = new TokenBucketRateLimiter(capacity, refillRate, clock.Now);
        return (limiter, clock);
    }

    [Fact]
    public void BucketStartsFull_AllowsBurstUpToCapacity()
    {
        var (limiter, _) = Build(capacity: 4);
        for (var i = 0; i < 4; i++) Assert.True(limiter.TryAcquire());
    }

    [Fact]
    public void RejectsRequest_WhenBucketIsEmpty()
    {
        var (limiter, _) = Build(capacity: 2);
        limiter.TryAcquire();
        limiter.TryAcquire();
        Assert.False(limiter.TryAcquire());
    }

    [Fact]
    public void Available_ReturnsRemainingTokens()
    {
        var (limiter, _) = Build(capacity: 5);
        Assert.Equal(5, limiter.Available);
        limiter.TryAcquire();
        Assert.Equal(4, limiter.Available);
    }

    [Fact]
    public void TokensRefill_AfterWaiting()
    {
        var (limiter, clock) = Build(capacity: 4, refillRate: 2.0);
        limiter.TryAcquire();
        limiter.TryAcquire();
        limiter.TryAcquire();
        limiter.TryAcquire();
        Assert.False(limiter.TryAcquire());    // empty

        clock.Advance(TimeSpan.FromSeconds(1)); // +2 tokens refilled
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());    // 2 tokens consumed, empty again
    }

    [Fact]
    public void Tokens_DoNotExceedCapacity()
    {
        var (limiter, clock) = Build(capacity: 3, refillRate: 10.0);
        limiter.TryAcquire(); // consume one
        clock.Advance(TimeSpan.FromSeconds(60)); // would be 600 tokens without cap
        Assert.Equal(3, limiter.Available);
    }

    [Fact]
    public void PartialSecond_RefillsProportionally()
    {
        var (limiter, clock) = Build(capacity: 10, refillRate: 4.0);
        // drain all 10
        for (var i = 0; i < 10; i++) limiter.TryAcquire();
        Assert.Equal(0, limiter.Available);

        clock.Advance(TimeSpan.FromMilliseconds(500)); // 0.5s × 4/s = 2 tokens
        Assert.Equal(2, limiter.Available);
    }

    [Fact]
    public void BurstThenRefill_AllowsContinuedTraffic()
    {
        var (limiter, clock) = Build(capacity: 5, refillRate: 1.0);
        for (var i = 0; i < 5; i++) limiter.TryAcquire(); // drain
        Assert.False(limiter.TryAcquire());

        clock.Advance(TimeSpan.FromSeconds(3)); // +3 tokens
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.False(limiter.TryAcquire());    // 3 consumed — empty again
    }

    [Fact]
    public void LimitProperty_ReturnsCapacity()
    {
        var (limiter, _) = Build(capacity: 8);
        Assert.Equal(8, limiter.Limit);
    }

    [Fact]
    public void Algorithm_IsTokenBucket()
    {
        var (limiter, _) = Build();
        Assert.Equal("Token Bucket", limiter.Algorithm);
    }
}

// ─── SlidingWindowRateLimiter ─────────────────────────────────────────────────

public class SlidingWindowRateLimiter_Tests
{
    private static (SlidingWindowRateLimiter Limiter, FakeClock Clock) Build(
        int limit = 3, int windowSeconds = 10)
    {
        var clock   = new FakeClock(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        var limiter = new SlidingWindowRateLimiter(limit, TimeSpan.FromSeconds(windowSeconds),
                                                   clock.Now);
        return (limiter, clock);
    }

    [Fact]
    public void AllowsRequests_UpToTheLimit()
    {
        var (limiter, _) = Build(limit: 3);
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
        Assert.True(limiter.TryAcquire());
    }

    [Fact]
    public void RejectsRequest_WhenLimitReached()
    {
        var (limiter, _) = Build(limit: 2);
        limiter.TryAcquire();
        limiter.TryAcquire();
        Assert.False(limiter.TryAcquire());
    }

    [Fact]
    public void OldTimestamps_AreEvicted_AllowingNewRequests()
    {
        var (limiter, clock) = Build(limit: 3, windowSeconds: 10);
        limiter.TryAcquire();
        limiter.TryAcquire();
        limiter.TryAcquire();
        Assert.False(limiter.TryAcquire());   // full

        clock.Advance(TimeSpan.FromSeconds(10)); // all three timestamps evicted
        Assert.True(limiter.TryAcquire());    // window is now empty
    }

    [Fact]
    public void Available_ReflectsCurrentWindowCount()
    {
        var (limiter, _) = Build(limit: 5);
        Assert.Equal(5, limiter.Available);
        limiter.TryAcquire();
        limiter.TryAcquire();
        Assert.Equal(3, limiter.Available);
    }

    [Fact]
    public void NoBoundaryBurst_UnlikeFixedWindow()
    {
        // Fixed Window would reset at t=10s and allow another full burst.
        // Sliding Window keeps the t=9 requests in the window until t=19.
        var (limiter, clock) = Build(limit: 5, windowSeconds: 10);

        clock.Advance(TimeSpan.FromSeconds(9)); // t=9
        for (var i = 0; i < 5; i++) limiter.TryAcquire(); // fill at t=9

        clock.Advance(TimeSpan.FromSeconds(1)); // t=10 — Fixed Window would reset
        Assert.False(limiter.TryAcquire());     // Sliding Window still blocks: t=9 is only 1s old
    }

    [Fact]
    public void RequestJustInsideWindow_IsNotEvicted()
    {
        var (limiter, clock) = Build(limit: 2, windowSeconds: 10);
        limiter.TryAcquire(); // at t=0

        clock.Advance(TimeSpan.FromSeconds(9)); // t=9; t=0 is 9s old — still in window
        Assert.Equal(1, limiter.Available);     // one slot left, t=0 not yet evicted
    }

    [Fact]
    public void PartialEviction_WorksCorrectly()
    {
        var (limiter, clock) = Build(limit: 3, windowSeconds: 10);
        limiter.TryAcquire(); // t=0
        clock.Advance(TimeSpan.FromSeconds(5));
        limiter.TryAcquire(); // t=5
        limiter.TryAcquire(); // t=5
        Assert.False(limiter.TryAcquire());   // full at t=5

        clock.Advance(TimeSpan.FromSeconds(5)); // t=10; cutoff=0; only t=0 evicted
        Assert.Equal(1, limiter.Available);     // t=5 requests still in window
        Assert.True(limiter.TryAcquire());
    }

    [Fact]
    public void Algorithm_IsSlidingWindow()
    {
        var (limiter, _) = Build();
        Assert.Equal("Sliding Window", limiter.Algorithm);
    }
}

// ─── ApiGateway ───────────────────────────────────────────────────────────────

public class ApiGateway_Tests
{
    private static ApiGateway BuildFixed(int limit = 3, int windowSeconds = 60)
    {
        var clock   = new FakeClock(DateTimeOffset.UtcNow);
        var limiter = new FixedWindowRateLimiter(limit, TimeSpan.FromSeconds(windowSeconds),
                                                 clock.Now);
        return new ApiGateway(limiter, "/api/test");
    }

    [Fact]
    public void HandleRequest_IncrementsHandled_OnSuccess()
    {
        var gw = BuildFixed(limit: 5);
        gw.HandleRequest();
        gw.HandleRequest();
        Assert.Equal(2, gw.RequestsHandled);
    }

    [Fact]
    public void HandleRequest_IncrementsRejected_OnRateLimit()
    {
        var gw = BuildFixed(limit: 1);
        gw.HandleRequest();
        gw.HandleRequest();
        gw.HandleRequest();
        Assert.Equal(2, gw.RequestsRejected);
    }

    [Fact]
    public void TotalRequests_IsHandledPlusRejected()
    {
        var gw = BuildFixed(limit: 2);
        gw.HandleRequest();
        gw.HandleRequest();
        gw.HandleRequest();
        Assert.Equal(3, gw.TotalRequests);
        Assert.Equal(2, gw.RequestsHandled);
        Assert.Equal(1, gw.RequestsRejected);
    }

    [Fact]
    public void HandleRequest_ReturnsFalse_WhenLimitExceeded()
    {
        var gw = BuildFixed(limit: 1);
        gw.HandleRequest();
        Assert.False(gw.HandleRequest());
    }

    [Fact]
    public void Available_ReflectsLimiterState()
    {
        var gw = BuildFixed(limit: 3);
        Assert.Equal(3, gw.Available);
        gw.HandleRequest();
        Assert.Equal(2, gw.Available);
    }

    [Fact]
    public void TwoGateways_AreIndependent()
    {
        var limiterA = new FixedWindowRateLimiter(2, TimeSpan.FromSeconds(60));
        var limiterB = new FixedWindowRateLimiter(2, TimeSpan.FromSeconds(60));
        var gwA = new ApiGateway(limiterA, "/api/a");
        var gwB = new ApiGateway(limiterB, "/api/b");

        gwA.HandleRequest();
        gwA.HandleRequest();
        gwA.HandleRequest(); // rejected on A

        Assert.Equal(1, gwA.RequestsRejected);
        Assert.Equal(0, gwB.RequestsRejected); // B unaffected
    }
}

// ─── Integration ─────────────────────────────────────────────────────────────

public class Integration_Tests
{
    [Fact]
    public void FixedWindow_FullLifecycle_ThreeWindows()
    {
        var clock   = new FakeClock(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        var limiter = new FixedWindowRateLimiter(3, TimeSpan.FromSeconds(10), clock.Now);
        var gw      = new ApiGateway(limiter, "/api/items");

        // Window 1: handle 3, reject 2
        for (var i = 0; i < 5; i++) gw.HandleRequest();
        Assert.Equal(3, gw.RequestsHandled);
        Assert.Equal(2, gw.RequestsRejected);

        // Window 2 reset
        clock.Advance(TimeSpan.FromSeconds(10));
        for (var i = 0; i < 3; i++) gw.HandleRequest();
        Assert.Equal(6, gw.RequestsHandled);

        // Window 3 reset
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.True(limiter.TryAcquire());
    }

    [Fact]
    public void TokenBucket_BurstThenSustainedTraffic()
    {
        var clock   = new FakeClock(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        var limiter = new TokenBucketRateLimiter(6, 2.0, clock.Now);
        var gw      = new ApiGateway(limiter, "/api/products");

        // Burst: drain all 6 tokens
        for (var i = 0; i < 6; i++) gw.HandleRequest();
        Assert.Equal(6, gw.RequestsHandled);
        Assert.False(gw.HandleRequest()); // exhausted

        // Sustained: 1 request per 0.5 s — each consumes 1 refilled token
        for (var i = 0; i < 4; i++)
        {
            clock.Advance(TimeSpan.FromMilliseconds(500)); // +1 token
            Assert.True(gw.HandleRequest());
        }
        Assert.Equal(10, gw.RequestsHandled);
    }

    [Fact]
    public void RateLimitExceededException_ContainsEndpointAndLimit()
    {
        var ex = new RateLimitExceededException("/api/search", 100);
        Assert.Equal("/api/search", ex.Endpoint);
        Assert.Equal(100,           ex.Limit);
        Assert.Contains("100",      ex.Message);
        Assert.Contains("/api/search", ex.Message);
    }
}
