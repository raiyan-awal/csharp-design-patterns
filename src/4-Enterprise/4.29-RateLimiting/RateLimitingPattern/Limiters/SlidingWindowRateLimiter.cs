using RateLimitingPattern.Core;

namespace RateLimitingPattern.Limiters;

// Tracks the exact timestamp of every request within a rolling window.
// On each call, expired timestamps are evicted from the front of the queue
// before the count is checked. This eliminates the boundary-burst problem
// of Fixed Window at the cost of O(limit) memory per limiter instance.
public sealed class SlidingWindowRateLimiter : IRateLimiter
{
    private readonly int _limit;
    private readonly TimeSpan _window;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Queue<DateTimeOffset> _timestamps = new();

    public SlidingWindowRateLimiter(int limit, TimeSpan window, Func<DateTimeOffset>? clock = null)
    {
        _limit  = limit;
        _window = window;
        _clock  = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public int Limit => _limit;
    public string Algorithm => "Sliding Window";

    public int Available
    {
        get { var now = _clock(); Evict(now); return Math.Max(0, _limit - _timestamps.Count); }
    }

    public bool TryAcquire()
    {
        var now = _clock();
        Evict(now);
        if (_timestamps.Count >= _limit) return false;
        _timestamps.Enqueue(now);
        return true;
    }

    // Removes timestamps that are at or beyond the window boundary —
    // those requests are no longer "within the last window duration".
    private void Evict(DateTimeOffset now)
    {
        var cutoff = now - _window;
        while (_timestamps.Count > 0 && _timestamps.Peek() <= cutoff)
            _timestamps.Dequeue();
    }
}
