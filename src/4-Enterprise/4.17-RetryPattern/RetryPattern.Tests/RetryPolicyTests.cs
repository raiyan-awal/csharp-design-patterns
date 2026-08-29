using RetryPattern.Core;
using RetryPattern.Services;

namespace RetryPattern.Tests;

// ── Helpers ───────────────────────────────────────────────────────────────

file static class Helpers
{
    public static RetryPolicy Make(
        int maxAttempts = 3,
        Func<int, TimeSpan>? delayStrategy = null,
        Func<Exception, bool>? shouldRetry = null,
        Action<Exception, int, TimeSpan>? onRetry = null,
        Action<TimeSpan>? sleep = null) =>
        new(new RetryOptions
        {
            MaxAttempts   = maxAttempts,
            DelayStrategy = delayStrategy ?? DelayStrategy.Fixed(TimeSpan.Zero),
            ShouldRetry   = shouldRetry   ?? (_ => true),
            OnRetry       = onRetry
        }, sleep ?? (_ => { }));

    // Returns a func that throws HttpRequestException `times` times, then returns `value`
    public static Func<int> FailThen(int times, int value = 42)
    {
        var calls = 0;
        return () =>
        {
            calls++;
            if (calls <= times) throw new HttpRequestException($"transient failure #{calls}");
            return value;
        };
    }
}

// ── Success on first attempt ──────────────────────────────────────────────

public class SuccessTests
{
    [Fact]
    public void FirstAttempt_ReturnsResult()
    {
        var policy = Helpers.Make();
        var result = policy.Execute(() => 42);
        Assert.Equal(42, result);
    }

    [Fact]
    public void FirstAttempt_DoesNotInvokeOnRetry()
    {
        var called = false;
        var policy = Helpers.Make(onRetry: (_, _, _) => called = true);
        policy.Execute(() => 1);
        Assert.False(called);
    }

    [Fact]
    public void FirstAttempt_DoesNotSleep()
    {
        var slept  = false;
        var policy = Helpers.Make(sleep: _ => slept = true);
        policy.Execute(() => 1);
        Assert.False(slept);
    }
}

// ── Retry on transient failure ────────────────────────────────────────────

public class RetryOnTransientFailureTests
{
    [Fact]
    public void SucceedsOnSecondAttempt_ReturnsResult()
    {
        var policy = Helpers.Make(maxAttempts: 3);
        var result = policy.Execute(Helpers.FailThen(times: 1, value: 99));
        Assert.Equal(99, result);
    }

    [Fact]
    public void SucceedsOnLastAttempt_ReturnsResult()
    {
        var policy = Helpers.Make(maxAttempts: 3);
        var result = policy.Execute(Helpers.FailThen(times: 2, value: 77));
        Assert.Equal(77, result);
    }

    [Fact]
    public void InvokesOnRetry_WithCorrectAttemptNumbers()
    {
        var attempts = new List<int>();
        var policy   = Helpers.Make(maxAttempts: 4, onRetry: (_, attempt, _) => attempts.Add(attempt));
        policy.Execute(Helpers.FailThen(times: 3));
        Assert.Equal([1, 2, 3], attempts);
    }

    [Fact]
    public void InvokesOnRetry_WithCorrectDelay()
    {
        var delays   = new List<TimeSpan>();
        var strategy = DelayStrategy.Fixed(TimeSpan.FromMilliseconds(250));
        var policy   = Helpers.Make(delayStrategy: strategy, maxAttempts: 3,
                                    onRetry: (_, _, delay) => delays.Add(delay));
        policy.Execute(Helpers.FailThen(times: 2));
        Assert.All(delays, d => Assert.Equal(250.0, d.TotalMilliseconds));
    }

    [Fact]
    public void SleepsForCalculatedDelay()
    {
        var sleptFor = new List<TimeSpan>();
        var strategy = DelayStrategy.Exponential(TimeSpan.FromMilliseconds(100));
        var policy   = Helpers.Make(maxAttempts: 3, delayStrategy: strategy,
                                    sleep: ts => sleptFor.Add(ts));
        policy.Execute(Helpers.FailThen(times: 2));
        Assert.Equal(2, sleptFor.Count);
        Assert.Equal(100.0, sleptFor[0].TotalMilliseconds);
        Assert.Equal(200.0, sleptFor[1].TotalMilliseconds);
    }
}

// ── Non-retryable exception ───────────────────────────────────────────────

public class NonRetryableExceptionTests
{
    private static RetryPolicy PolicyThatOnlyRetries<TRetryable>() where TRetryable : Exception =>
        Helpers.Make(shouldRetry: ex => ex is TRetryable);

    [Fact]
    public void NonRetryableException_DoesNotRetry()
    {
        var callCount = 0;
        var policy    = PolicyThatOnlyRetries<HttpRequestException>();
        Assert.Throws<InvalidOperationException>(() =>
            policy.Execute<int>(() =>
            {
                callCount++;
                throw new InvalidOperationException("not retryable");
            }));
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void NonRetryableException_PropagatesOriginalException()
    {
        var policy = PolicyThatOnlyRetries<HttpRequestException>();
        var ex     = Assert.Throws<InvalidOperationException>(() =>
            policy.Execute<int>(() => throw new InvalidOperationException("original message")));
        Assert.Equal("original message", ex.Message);
    }

    [Fact]
    public void NonRetryableException_DoesNotSleep()
    {
        var slept  = false;
        var policy = Helpers.Make(shouldRetry: ex => ex is HttpRequestException,
                                  sleep: _ => slept = true);
        try { policy.Execute<int>(() => throw new InvalidOperationException()); } catch { }
        Assert.False(slept);
    }
}

// ── Retries exhausted ─────────────────────────────────────────────────────

public class ExhaustedRetriesTests
{
    [Fact]
    public void AllAttemptsFail_ThrowsRetryExhaustedException()
    {
        var policy = Helpers.Make(maxAttempts: 3);
        Assert.Throws<RetryExhaustedException>(() =>
            policy.Execute<int>(() => throw new HttpRequestException("fail")));
    }

    [Fact]
    public void AllAttemptsFail_AttemptsExactMaxTimes()
    {
        var callCount = 0;
        var policy    = Helpers.Make(maxAttempts: 4);
        try
        {
            policy.Execute<int>(() =>
            {
                callCount++;
                throw new HttpRequestException("fail");
            });
        }
        catch (RetryExhaustedException) { }
        Assert.Equal(4, callCount);
    }

    [Fact]
    public void AllAttemptsFail_InnerExceptionIsLastThrown()
    {
        var callCount = 0;
        var policy    = Helpers.Make(maxAttempts: 3);
        var rex       = Assert.Throws<RetryExhaustedException>(() =>
            policy.Execute<int>(() =>
            {
                callCount++;
                throw new HttpRequestException($"failure #{callCount}");
            }));
        Assert.Equal("failure #3", rex.InnerException!.Message);
    }

    [Fact]
    public void AllAttemptsFail_InvokesOnRetry_MaxMinusOneTimes()
    {
        var retryCount = 0;
        var policy     = Helpers.Make(maxAttempts: 4, onRetry: (_, _, _) => retryCount++);
        try { policy.Execute<int>(() => throw new HttpRequestException("fail")); } catch { }
        Assert.Equal(3, retryCount);  // 4 attempts → OnRetry fires between each, not after the last
    }
}

// ── Delay strategy ────────────────────────────────────────────────────────

public class DelayStrategyTests
{
    [Fact]
    public void Fixed_ReturnsConstantDelay()
    {
        var strategy = DelayStrategy.Fixed(TimeSpan.FromSeconds(5));
        Assert.Equal(5000.0, strategy(1).TotalMilliseconds);
        Assert.Equal(5000.0, strategy(2).TotalMilliseconds);
        Assert.Equal(5000.0, strategy(5).TotalMilliseconds);
    }

    [Fact]
    public void Exponential_Attempt1_ReturnsBaseDelay()
    {
        var strategy = DelayStrategy.Exponential(TimeSpan.FromMilliseconds(100));
        Assert.Equal(100.0, strategy(1).TotalMilliseconds);
    }

    [Fact]
    public void Exponential_Attempt2_DoublesBaseDelay()
    {
        var strategy = DelayStrategy.Exponential(TimeSpan.FromMilliseconds(100));
        Assert.Equal(200.0, strategy(2).TotalMilliseconds);
    }

    [Fact]
    public void Exponential_Attempt3_QuadruplesBaseDelay()
    {
        var strategy = DelayStrategy.Exponential(TimeSpan.FromMilliseconds(100));
        Assert.Equal(400.0, strategy(3).TotalMilliseconds);
    }

    [Fact]
    public void ExponentialWithJitter_StaysWithinJitterRange()
    {
        var rng      = new Random(42);
        var strategy = DelayStrategy.ExponentialWithJitter(TimeSpan.FromMilliseconds(100), rng);
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var exponential = 100.0 * Math.Pow(2, attempt - 1);
            var actual      = strategy(attempt).TotalMilliseconds;
            Assert.InRange(actual, exponential * 0.75, exponential * 1.25);
        }
    }
}
