using CircuitBreakerPattern.Core;
using CircuitBreakerPattern.Services;

namespace CircuitBreakerPattern.Tests;

// ── Helpers ───────────────────────────────────────────────────────────────

file static class Helpers
{
    public static CircuitBreaker Make(
        int failureThreshold = 3,
        int successThreshold = 2,
        Func<DateTime>? clock = null) =>
        new(new CircuitBreakerOptions
        {
            FailureThreshold = failureThreshold,
            SuccessThreshold = successThreshold,
            ResetTimeout     = TimeSpan.FromSeconds(30)
        }, clock);

    public static void Fail(CircuitBreaker cb, int times)
    {
        for (var i = 0; i < times; i++)
            try { cb.Execute<int>(() => throw new Exception("simulated")); } catch { }
    }

    public static void Succeed(CircuitBreaker cb, int times)
    {
        for (var i = 0; i < times; i++)
            cb.Execute(() => 1);
    }
}

// ── Closed state ─────────────────────────────────────────────────────────

public class ClosedStateTests
{
    [Fact]
    public void InitialState_IsClosed()
    {
        var cb = Helpers.Make();
        Assert.Equal(CircuitState.Closed, cb.State);
    }

    [Fact]
    public void Closed_SuccessfulCall_ReturnsResult()
    {
        var cb     = Helpers.Make();
        var result = cb.Execute(() => 42);
        Assert.Equal(42, result);
    }

    [Fact]
    public void Closed_FailuresBelowThreshold_RemainsClosedAndTracksCount()
    {
        var cb = Helpers.Make(failureThreshold: 3);
        Helpers.Fail(cb, 2);
        Assert.Equal(CircuitState.Closed, cb.State);
        Assert.Equal(2, cb.FailureCount);
    }

    [Fact]
    public void Closed_FailuresReachThreshold_OpensCircuit()
    {
        var cb = Helpers.Make(failureThreshold: 3);
        Helpers.Fail(cb, 3);
        Assert.Equal(CircuitState.Open, cb.State);
    }

    [Fact]
    public void Closed_SuccessAfterPartialFailures_ResetsFailureCount()
    {
        var cb = Helpers.Make(failureThreshold: 3);
        Helpers.Fail(cb, 2);
        Helpers.Succeed(cb, 1);
        Assert.Equal(0, cb.FailureCount);
        Assert.Equal(CircuitState.Closed, cb.State);
    }
}

// ── Open state ────────────────────────────────────────────────────────────

public class OpenStateTests
{
    private static CircuitBreaker OpenCircuit(Func<DateTime>? clock = null)
    {
        var cb = Helpers.Make(failureThreshold: 3, clock: clock);
        Helpers.Fail(cb, 3);
        return cb;
    }

    [Fact]
    public void Open_ThrowsCircuitBreakerOpenException()
    {
        var cb = OpenCircuit();
        Assert.Throws<CircuitBreakerOpenException>(() => cb.Execute(() => 1));
    }

    [Fact]
    public void Open_DoesNotCallUnderlyingAction()
    {
        var cb      = OpenCircuit();
        var called  = false;
        try { cb.Execute(() => { called = true; return 1; }); } catch { }
        Assert.False(called);
    }

    [Fact]
    public void Open_BeforeTimeout_RemainsOpen()
    {
        var now = DateTime.UtcNow;
        var cb  = OpenCircuit(clock: () => now);  // frozen clock
        try { cb.Execute(() => 1); } catch { }
        Assert.Equal(CircuitState.Open, cb.State);
    }

    [Fact]
    public void Open_AfterTimeout_TransitionsToHalfOpen()
    {
        var now = DateTime.UtcNow;
        var cb  = new CircuitBreaker(new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            SuccessThreshold = 2,
            ResetTimeout     = TimeSpan.FromSeconds(30)
        }, () => now);

        Helpers.Fail(cb, 3);           // open with frozen clock
        now = now.AddSeconds(31);      // advance clock past timeout

        // Next Execute should transition to HalfOpen then succeed (service healthy)
        var result = cb.Execute(() => 99);
        Assert.Equal(99, result);
        Assert.Equal(CircuitState.HalfOpen, cb.State);  // 1 success, needs 2 to close
    }
}

// ── Half-Open state ───────────────────────────────────────────────────────

public class HalfOpenStateTests
{
    private static (CircuitBreaker cb, DateTime refTime) HalfOpenCircuit()
    {
        var now = DateTime.UtcNow;
        var cb  = new CircuitBreaker(new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            SuccessThreshold = 2,
            ResetTimeout     = TimeSpan.FromSeconds(30)
        }, () => now);
        Helpers.Fail(cb, 3);
        now = now.AddSeconds(31);
        // Trigger transition to HalfOpen with a successful call
        cb.Execute(() => 1);
        return (cb, now);
    }

    [Fact]
    public void HalfOpen_FirstSuccessDoesNotClose()
    {
        var (cb, _) = HalfOpenCircuit();
        Assert.Equal(CircuitState.HalfOpen, cb.State);
    }

    [Fact]
    public void HalfOpen_SuccessesReachThreshold_ClosesCircuit()
    {
        var (cb, _) = HalfOpenCircuit();
        // Already have 1 success from HalfOpenCircuit; need 1 more (threshold = 2)
        cb.Execute(() => 1);
        Assert.Equal(CircuitState.Closed, cb.State);
    }

    [Fact]
    public void HalfOpen_Failure_ReopensCircuit()
    {
        var (cb, _) = HalfOpenCircuit();
        try { cb.Execute<int>(() => throw new Exception("still broken")); } catch { }
        Assert.Equal(CircuitState.Open, cb.State);
    }

    [Fact]
    public void HalfOpen_AfterClose_ResetsFailureCount()
    {
        var (cb, _) = HalfOpenCircuit();
        cb.Execute(() => 1);  // second success → Closed
        Assert.Equal(0, cb.FailureCount);
        Assert.Equal(CircuitState.Closed, cb.State);
    }
}

// ── Integration ───────────────────────────────────────────────────────────

public class IntegrationTests
{
    [Fact]
    public void FullLifecycle_ClosedToOpenToHalfOpenToClosed()
    {
        var now = DateTime.UtcNow;
        var cb  = new CircuitBreaker(new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            SuccessThreshold = 2,
            ResetTimeout     = TimeSpan.FromSeconds(30)
        }, () => now);

        // Closed → fail until open
        Helpers.Fail(cb, 3);
        Assert.Equal(CircuitState.Open, cb.State);

        // Open → advance time → HalfOpen on first call
        now = now.AddSeconds(31);
        cb.Execute(() => 1);
        Assert.Equal(CircuitState.HalfOpen, cb.State);

        // HalfOpen → second success → Closed
        cb.Execute(() => 1);
        Assert.Equal(CircuitState.Closed, cb.State);
    }

    [Fact]
    public void Execute_PropagatesOriginalException()
    {
        var cb = Helpers.Make(failureThreshold: 10);
        var ex = Assert.Throws<InvalidOperationException>(
            () => cb.Execute<int>(() => throw new InvalidOperationException("domain error")));
        Assert.Equal("domain error", ex.Message);
    }

    [Fact]
    public void WithShippingService_OpenCircuit_BlocksCallsToService()
    {
        var svc = new SimulatedShippingRateService();
        var cb  = Helpers.Make(failureThreshold: 3);

        svc.SetFailing();
        Helpers.Fail(cb, 3);  // open the circuit

        var countBefore = svc.CallCount;
        try { cb.Execute(() => svc.GetRate("Toronto, ON", "Vancouver, BC", 1m)); } catch { }
        try { cb.Execute(() => svc.GetRate("Toronto, ON", "Vancouver, BC", 1m)); } catch { }

        Assert.Equal(countBefore, svc.CallCount);  // no new calls reached the service
    }

    [Fact]
    public void WithShippingService_AfterRecovery_ReturnsValidRate()
    {
        var svc = new SimulatedShippingRateService();
        var now = DateTime.UtcNow;
        var cb  = new CircuitBreaker(new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            SuccessThreshold = 2,
            ResetTimeout     = TimeSpan.FromSeconds(30)
        }, () => now);

        svc.SetFailing();
        Helpers.Fail(cb, 3);

        now = now.AddSeconds(31);
        svc.SetHealthy();

        var rate1 = cb.Execute(() => svc.GetRate("Toronto, ON", "Vancouver, BC", 1m));
        var rate2 = cb.Execute(() => svc.GetRate("Montreal, QC", "Calgary, AB",  0.5m));

        Assert.Equal(CircuitState.Closed, cb.State);
        Assert.True(rate1.PriceCAD > 0);
        Assert.True(rate2.PriceCAD > 0);
    }

    [Fact]
    public void ClosedCircuit_CanReopenAfterSecondFailureBurst()
    {
        var cb = Helpers.Make(failureThreshold: 3);

        // First burst — open
        Helpers.Fail(cb, 3);
        Assert.Equal(CircuitState.Open, cb.State);

        // Would need clock injection to test full re-open from closed;
        // verify failure count resets on close
        var now = DateTime.UtcNow;
        var cb2 = new CircuitBreaker(new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            SuccessThreshold = 2,
            ResetTimeout     = TimeSpan.FromSeconds(30)
        }, () => now);

        Helpers.Fail(cb2, 3);
        now = now.AddSeconds(31);
        Helpers.Succeed(cb2, 2);
        Assert.Equal(CircuitState.Closed, cb2.State);

        // Second burst should re-open from fresh failure count
        Helpers.Fail(cb2, 3);
        Assert.Equal(CircuitState.Open, cb2.State);
    }
}
