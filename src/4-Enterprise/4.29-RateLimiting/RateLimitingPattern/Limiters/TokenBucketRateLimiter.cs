using RateLimitingPattern.Core;

namespace RateLimitingPattern.Limiters;

// Maintains a bucket of tokens. Tokens are consumed on each request and
// refilled at a constant rate. Permits controlled bursting — callers can
// consume accumulated tokens instantly — while enforcing a sustainable
// average rate equal to the refill rate.
public sealed class TokenBucketRateLimiter : IRateLimiter
{
    private readonly int _capacity;
    private readonly double _refillRatePerSecond;
    private readonly Func<DateTimeOffset> _clock;
    private double _tokens;
    private DateTimeOffset _lastRefill = DateTimeOffset.MinValue;

    public TokenBucketRateLimiter(int capacity, double refillRatePerSecond,
                                  Func<DateTimeOffset>? clock = null)
    {
        _capacity           = capacity;
        _refillRatePerSecond = refillRatePerSecond;
        _tokens             = capacity;       // bucket starts full
        _clock              = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public int Limit => _capacity;
    public string Algorithm => "Token Bucket";
    public int Available { get { Refill(); return (int)Math.Floor(_tokens); } }

    public bool TryAcquire()
    {
        Refill();
        if (_tokens < 1) return false;
        _tokens -= 1;
        return true;
    }

    private void Refill()
    {
        var now = _clock();
        if (_lastRefill == DateTimeOffset.MinValue)
        {
            _lastRefill = now;
            return;
        }
        var elapsed = (now - _lastRefill).TotalSeconds;
        if (elapsed <= 0) return;
        _tokens     = Math.Min(_capacity, _tokens + elapsed * _refillRatePerSecond);
        _lastRefill = now;
    }
}
