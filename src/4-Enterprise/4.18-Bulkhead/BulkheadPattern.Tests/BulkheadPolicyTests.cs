using BulkheadPattern.Core;

namespace BulkheadPattern.Tests;

// ── Helpers ───────────────────────────────────────────────────────────────

file static class Helpers
{
    // Holds a bulkhead slot open until release.Set() is called; signals holding when inside.
    public static Task HoldSlot(BulkheadPolicy policy, CountdownEvent holding, ManualResetEventSlim release) =>
        Task.Run(() => policy.Execute(() =>
        {
            holding.Signal();
            release.Wait();
            return 1;
        }));
}

// ── Single-threaded behaviour ─────────────────────────────────────────────

public class SingleThreadedBehaviorTests
{
    [Fact]
    public void Execute_ReturnsResult_WhenUnderCapacity()
    {
        var policy = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 3 });
        Assert.Equal(42, policy.Execute(() => 42));
    }

    [Fact]
    public void Execute_ReleasesSlot_AfterSuccess()
    {
        var policy = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 1 });
        policy.Execute(() => 1);
        var result = policy.Execute(() => 99);  // would deadlock if slot was not released
        Assert.Equal(99, result);
    }

    [Fact]
    public void Execute_ReleasesSlot_AfterException()
    {
        var policy = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 1 });
        try { policy.Execute<int>(() => throw new InvalidOperationException("boom")); } catch { }
        var result = policy.Execute(() => 77);  // must succeed — slot must have been released
        Assert.Equal(77, result);
    }

    [Fact]
    public void Execute_PropagatesOriginalException()
    {
        var policy = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 2 });
        var ex = Assert.Throws<InvalidOperationException>(() =>
            policy.Execute<int>(() => throw new InvalidOperationException("original")));
        Assert.Equal("original", ex.Message);
    }

    [Fact]
    public void Available_ReflectsRemainingSlots()
    {
        var policy = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 3 });
        Assert.Equal(3, policy.Available);
    }
}

// ── No-queue rejection (MaxQueueSize = 0) ────────────────────────────────

public class NoQueueRejectionTests
{
    [Fact]
    public void NoQueue_RejectsImmediately_WhenAllSlotsBusy()
    {
        var policy  = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 2, MaxQueueSize = 0 });
        var holding = new CountdownEvent(2);
        var release = new ManualResetEventSlim(false);

        var t1 = Helpers.HoldSlot(policy, holding, release);
        var t2 = Helpers.HoldSlot(policy, holding, release);
        holding.Wait();  // both slots occupied

        Assert.Throws<BulkheadRejectedException>(() => policy.Execute(() => 1));

        release.Set();
        Task.WaitAll(t1, t2);
    }

    [Fact]
    public void NoQueue_AllowsCall_AfterSlotFreed()
    {
        var policy = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 1, MaxQueueSize = 0 });
        policy.Execute(() => 1);           // acquires and releases the slot
        var result = policy.Execute(() => 99);  // slot free again
        Assert.Equal(99, result);
    }

    [Fact]
    public void NoQueue_ThrowsBulkheadRejectedException()
    {
        var policy  = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 1, MaxQueueSize = 0 });
        var holding = new CountdownEvent(1);
        var release = new ManualResetEventSlim(false);
        var holder  = Helpers.HoldSlot(policy, holding, release);
        holding.Wait();

        Assert.Throws<BulkheadRejectedException>(() => policy.Execute(() => 1));

        release.Set();
        holder.Wait();
    }
}

// ── Queue behaviour (MaxQueueSize > 0) ───────────────────────────────────

public class QueueBehaviorTests
{
    [Fact]
    public void WithQueue_QueuedCall_EventuallySucceeds()
    {
        var policy  = new BulkheadPolicy(new BulkheadOptions
        {
            MaxConcurrency = 1,
            MaxQueueSize   = 1,
            QueueTimeout   = TimeSpan.FromSeconds(5)
        });
        var holding = new CountdownEvent(1);
        var release = new ManualResetEventSlim(false);
        var holder  = Helpers.HoldSlot(policy, holding, release);
        holding.Wait();  // slot occupied

        // Queue a waiter
        var waiter = Task.Run(() => policy.Execute(() => 99));

        // Release the holder — waiter should now acquire and complete
        release.Set();
        holder.Wait();
        Assert.Equal(99, waiter.Result);
    }

    [Fact]
    public void WithQueue_RejectsCall_WhenQueueFull()
    {
        var policy  = new BulkheadPolicy(new BulkheadOptions
        {
            MaxConcurrency = 1,
            MaxQueueSize   = 1,
            QueueTimeout   = TimeSpan.FromSeconds(5)
        });
        var holding = new CountdownEvent(1);
        var release = new ManualResetEventSlim(false);
        var holder  = Helpers.HoldSlot(policy, holding, release);
        holding.Wait();  // slot occupied

        // Fill the queue
        var queued = Task.Run(() => policy.Execute(() => 1));
        Thread.Sleep(30);  // give the queued task time to enter the semaphore wait

        // Queue is now full (MaxQueueSize = 1) — next call should be rejected
        Assert.Throws<BulkheadRejectedException>(() => policy.Execute(() => 1));

        release.Set();
        Task.WaitAll(holder, queued);
    }

    [Fact]
    public void WithQueue_TimesOut_WhenQueueTimeoutExceeded()
    {
        var policy  = new BulkheadPolicy(new BulkheadOptions
        {
            MaxConcurrency = 1,
            MaxQueueSize   = 1,
            QueueTimeout   = TimeSpan.FromMilliseconds(50)
        });
        var holding = new CountdownEvent(1);
        var release = new ManualResetEventSlim(false);
        var holder  = Helpers.HoldSlot(policy, holding, release);
        holding.Wait();

        // Waiter should time out after 50ms
        Assert.Throws<BulkheadRejectedException>(() => policy.Execute(() => 1));

        release.Set();
        holder.Wait();
    }

    [Fact]
    public void WithQueue_AllCallsSucceed_WhenWithinCombinedCapacity()
    {
        var policy  = new BulkheadPolicy(new BulkheadOptions
        {
            MaxConcurrency = 2,
            MaxQueueSize   = 2,
            QueueTimeout   = TimeSpan.FromSeconds(5)
        });
        var gate    = new CountdownEvent(4);
        var results = new int[4];

        var tasks = Enumerable.Range(0, 4).Select(i => Task.Run(() =>
        {
            gate.Signal();
            gate.Wait();
            results[i] = policy.Execute(() => { Thread.Sleep(50); return i + 1; });
        })).ToArray();

        Task.WaitAll(tasks);
        Assert.Equal([1, 2, 3, 4], results.Order().ToArray());
    }
}

// ── Concurrent correctness ────────────────────────────────────────────────

public class ConcurrentCorrectnessTests
{
    [Fact]
    public void Concurrent_ExcessCalls_AreRejected_WithNoQueue()
    {
        var policy   = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 2, MaxQueueSize = 0 });
        var holding  = new CountdownEvent(2);
        var release  = new ManualResetEventSlim(false);
        var rejected = 0;

        var holders = Enumerable.Range(0, 2).Select(_ => Helpers.HoldSlot(policy, holding, release)).ToArray();
        holding.Wait();

        var extras = Enumerable.Range(0, 3).Select(_ => Task.Run(() =>
        {
            try   { policy.Execute(() => 1); }
            catch (BulkheadRejectedException) { Interlocked.Increment(ref rejected); }
        })).ToArray();

        Task.WaitAll(extras);
        Assert.Equal(3, rejected);

        release.Set();
        Task.WaitAll(holders);
    }

    [Fact]
    public void Concurrent_ExactlyMaxConcurrency_CallsAllSucceed()
    {
        const int max    = 4;
        var policy       = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = max, MaxQueueSize = 0 });
        var gate         = new CountdownEvent(max);
        var succeeded    = 0;

        var tasks = Enumerable.Range(0, max).Select(_ => Task.Run(() =>
        {
            gate.Signal();
            gate.Wait();
            try   { policy.Execute(() => { Interlocked.Increment(ref succeeded); return 1; }); }
            catch (BulkheadRejectedException) { }
        })).ToArray();

        Task.WaitAll(tasks);
        Assert.Equal(max, succeeded);
    }

    [Fact]
    public void Concurrent_TwoBulkheads_SaturatingOne_DoesNotAffectOther()
    {
        var bulkheadA = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 1, MaxQueueSize = 0 });
        var bulkheadB = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 1, MaxQueueSize = 0 });
        var holding   = new CountdownEvent(1);
        var release   = new ManualResetEventSlim(false);

        // Saturate bulkhead A
        var holderA = Helpers.HoldSlot(bulkheadA, holding, release);
        holding.Wait();

        // Bulkhead A is full
        Assert.Throws<BulkheadRejectedException>(() => bulkheadA.Execute(() => 1));

        // Bulkhead B is completely independent — must still accept calls
        var resultB = bulkheadB.Execute(() => 99);
        Assert.Equal(99, resultB);

        release.Set();
        holderA.Wait();
    }
}
