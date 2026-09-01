using System.Diagnostics.CodeAnalysis;

namespace CacheAsidePattern.Core;

public interface ICache<TKey, TValue> where TKey : notnull
{
    bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value);
    void Set(TKey key, TValue value, TimeSpan? ttl = null);
    void Remove(TKey key);
    void Clear();
    int Count { get; }
    int Hits { get; }
    int Misses { get; }
}
