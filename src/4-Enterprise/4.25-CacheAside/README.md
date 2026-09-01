# 4.25 — Cache-Aside

## Intent

Cache-Aside (also called Lazy Loading) positions the application in control of its own cache. On a read, the application checks the cache first; on a miss it loads from the backing store, populates the cache, and returns the data. On a write it persists to the store and invalidates the cached copy so the next read reloads fresh data. The cache is never the primary source of truth — it is a transparent performance layer.

## The Problem It Solves

Without caching, every read hits the backing store regardless of how recently the data was fetched:

```csharp
// Without Cache-Aside: every call goes to the database
public Book? GetById(string id)
{
    return _repository.FindById(id);  // DB query on every call
}
```

Problems this creates:
- **Latency** — every call pays the full cost of a database round-trip, even for data that never changes between requests.
- **Load** — a popular record is read from the database hundreds of times per second instead of once.
- **Scalability** — the database becomes the bottleneck as traffic grows; adding application servers does not help.

## Solution: Check Cache First, Fall Back to Store

```csharp
public Book? GetById(string id)
{
    var key = $"book:{id}";
    if (_bookCache.TryGet(key, out var cached))
        return cached;          // cache hit — no DB query

    var book = _repository.FindById(id);
    if (book is not null)
        _bookCache.Set(key, book);  // populate cache
    return book;
}
```

On a write, invalidate the cached copy so the next read reloads from the store:

```csharp
public void Save(Book book)
{
    _repository.Save(book);
    _bookCache.Remove($"book:{book.Id}");  // evict stale entry
}
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Cache interface | `ICache<TKey, TValue>` | TryGet / Set / Remove / Clear / TTL / Hits / Misses |
| Concrete cache | `InMemoryCache<TKey, TValue>` | Dictionary-backed; injectable clock; per-entry TTL; metrics |
| Repository interface | `IBookRepository` | FindById, FindByAuthor, Save, Delete |
| Concrete repository | `InMemoryBookRepository` | Simulates the backing store; exposes `LoadCount` to verify DB hits |
| Service | `BookCatalogueService` | Implements cache-aside logic for reads and write-invalidation |

## Structure

```
4.25-CacheAside/
├── CacheAsidePattern/
│   ├── Core/
│   │   ├── ICache.cs                    ← TryGet ([MaybeNullWhen(false)]), Set (ttl?), Remove, Clear, Count, Hits, Misses
│   │   └── InMemoryCache.cs             ← Dictionary<TKey,CacheEntry> + Lock; injectable Func<DateTimeOffset> clock
│   ├── Domain/
│   │   └── Book.cs                      ← sealed record (Id, Title, Author, Isbn, PriceCAD, Genre)
│   ├── Data/
│   │   ├── IBookRepository.cs           ← FindById, FindByAuthor, Save, Delete
│   │   └── InMemoryBookRepository.cs    ← LoadCount tracks DB hits for demo and tests
│   ├── Services/
│   │   └── BookCatalogueService.cs      ← GetById, GetByAuthor (read-through); Save, Delete (write-invalidate)
│   └── Program.cs
└── CacheAsidePattern.Tests/
    └── CacheAsidePatternTests.cs        ← 26 tests across 6 suites
```

## Key Code

### ICache — minimal, generic contract

```csharp
public interface ICache<TKey, TValue> where TKey : notnull
{
    bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value);
    void Set(TKey key, TValue value, TimeSpan? ttl = null);
    void Remove(TKey key);
    void Clear();
    int Count { get; }
    int Hits  { get; }
    int Misses { get; }
}
```

`[MaybeNullWhen(false)]` tells the compiler that `value` is guaranteed non-null when the method returns `true`, so callers do not need a null-check after a successful `TryGet`.

### InMemoryCache — injectable clock for deterministic TTL tests

```csharp
public sealed class InMemoryCache<TKey, TValue> : ICache<TKey, TValue>
    where TKey : notnull
{
    private sealed record CacheEntry(TValue Value, DateTimeOffset? ExpiresAt);
    private readonly Dictionary<TKey, CacheEntry> _store = [];
    private readonly Func<DateTimeOffset> _clock;

    public InMemoryCache(Func<DateTimeOffset>? clock = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        lock (_lock)
        {
            if (_store.TryGetValue(key, out var entry) &&
                (entry.ExpiresAt is null || entry.ExpiresAt > _clock()))
            {
                value = entry.Value; Hits++; return true;
            }
            _store.Remove(key);
            value = default; Misses++; return false;
        }
    }
}
```

The clock is a `Func<DateTimeOffset>` defaulting to `DateTimeOffset.UtcNow`. In tests, a `FakeClock` is injected so TTL expiry can be tested by advancing time without sleeping.

### BookCatalogueService — read-through and write-invalidate

```csharp
public Book? GetById(string id)
{
    var key = $"book:{id}";
    if (bookCache.TryGet(key, out var cached)) return cached;

    var book = repository.FindById(id);
    if (book is not null) bookCache.Set(key, book);
    return book;
}

public void Save(Book book)
{
    repository.Save(book);
    bookCache.Remove($"book:{book.Id}");
    listCache.Clear();   // author lists may be stale after any write
}
```

The application owns the cache logic entirely. The repository and the cache are independent — neither knows about the other.

## Demo Scenarios

```
1. Cold cache          — first lookups for two books both miss; DB LoadCount increments twice
2. Warm cache          — same books fetched again; zero new DB queries, all served from cache
3. Author list caching — first call loads from DB; second call is a hit; list cache tracks hits
4. Write invalidation  — Save() with a new price evicts the cached entry; next GetById reloads
5. TTL expiry          — FakeClock advanced past the 5-minute TTL; TryGet returns false
6. Metrics summary     — total DB queries, hits, misses, and live entry counts
```

## When to Use

- Reads are far more frequent than writes, and the same data is requested repeatedly.
- Data can tolerate a brief window of staleness between a write and the next cache refresh.
- You want a simple, application-controlled caching strategy without a write-through or read-through infrastructure layer.
- You need per-entry TTL to bound the maximum staleness of any cached value.

## When NOT to Use

- Your data changes frequently and cache invalidation on every write negates the benefit.
- Strict consistency is required — Cache-Aside allows a window of stale reads between a write and the next cache miss.
- You need cache warming at startup — Cache-Aside is lazy; the cache starts cold and is populated on demand.
- You have a very large number of distinct keys with low re-access rates — cached entries never get a hit and waste memory.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Reduced latency | Cache hits avoid the round-trip to the backing store entirely |
| Reduced load | Frequently read records are served from memory; the DB sees only cache misses |
| Resilience | The cache is a performance optimisation; if it fails, reads fall back to the store |
| Flexible TTL | Different entries can carry different expiry windows to match their staleness tolerance |
| Testability | Injectable clock makes TTL expiry deterministic in unit tests with no sleeps |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Cold start | The first request for any key always pays the full store cost; cache warms gradually |
| Stale reads | A write evicts the entry, but a concurrent reader may have already fetched the stale copy a moment before |
| Thundering herd | If a popular entry expires, many concurrent requests may simultaneously miss and all hit the store at once |
| Invalidation complexity | When a write affects multiple cache keys (e.g., author lists), every affected key must be evicted |

## Related Patterns

- **Repository (4.01)** — the backing store abstracted behind `IBookRepository`; Cache-Aside wraps the repository without changing its interface.
- **Proxy (2.7)** — a caching proxy also intercepts reads and returns cached data, but the proxy is transparent to the caller; Cache-Aside requires the application to explicitly check the cache.
- **Read Model / Projection (4.28)** — a pre-built, query-optimised read model is another way to avoid expensive reads, but it is updated by events rather than populated on demand.
- **Circuit Breaker (4.16)** — often paired with Cache-Aside: if the backing store is unavailable, serve stale cache data rather than failing entirely.

## Running the Demo

```bash
cd src/4-Enterprise/4.25-CacheAside/CacheAsidePattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.25-CacheAside/CacheAsidePattern.Tests
dotnet test
```
