using RateLimitingPattern.Core;

namespace RateLimitingPattern.Limiters;

// Counts requests in a fixed time window. When the window expires the
// counter resets to zero and a new window begins. Simple and predictable,
// but can allow up to 2× the limit in a burst across a window boundary.
public sealed class FixedWindowRateLimiter : IRateLimiter
{
    private readonly int _limit;
    private readonly TimeSpan _window;
    private readonly Func<DateTimeOffset> _clock;
    private int _count;
    private DateTimeOffset _windowStart = DateTimeOffset.MinValue;

    public FixedWindowRateLimiter(int limit, TimeSpan window, Func<DateTimeOffset>? clock = null)
    {
        _limit  = limit;
        _window = window;
        _clock  = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public int Limit => _limit;
    public string Algorithm => "Fixed Window";

    public int Available
    {
        get { RefreshIfExpired(); return Math.Max(0, _limit - _count); }
    }

    public bool TryAcquire()
    {
        RefreshIfExpired();
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
}
