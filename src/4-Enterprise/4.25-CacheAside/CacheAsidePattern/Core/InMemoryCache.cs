using System.Diagnostics.CodeAnalysis;

namespace CacheAsidePattern.Core;

public sealed class InMemoryCache<TKey, TValue> : ICache<TKey, TValue>
    where TKey : notnull
{
    private sealed record CacheEntry(TValue Value, DateTimeOffset? ExpiresAt);

    private readonly Dictionary<TKey, CacheEntry> _store = [];
    private readonly Lock _lock = new();
    private readonly Func<DateTimeOffset> _clock;

    public InMemoryCache(Func<DateTimeOffset>? clock = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public int Hits { get; private set; }
    public int Misses { get; private set; }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                var now = _clock();
                return _store.Count(e => e.Value.ExpiresAt is null || e.Value.ExpiresAt > now);
            }
        }
    }

    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        lock (_lock)
        {
            if (_store.TryGetValue(key, out var entry) &&
                (entry.ExpiresAt is null || entry.ExpiresAt > _clock()))
            {
                value = entry.Value;
                Hits++;
                return true;
            }

            _store.Remove(key);
            value = default;
            Misses++;
            return false;
        }
    }

    public void Set(TKey key, TValue value, TimeSpan? ttl = null)
    {
        lock (_lock)
        {
            var expiresAt = ttl.HasValue ? _clock().Add(ttl.Value) : (DateTimeOffset?)null;
            _store[key] = new CacheEntry(value, expiresAt);
        }
    }

    public void Remove(TKey key)
    {
        lock (_lock) _store.Remove(key);
    }

    public void Clear()
    {
        lock (_lock) _store.Clear();
    }
}
