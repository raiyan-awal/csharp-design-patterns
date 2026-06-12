# 2.7 Proxy Pattern

## Intent

Provide a surrogate or placeholder for another object to control access to it. The proxy implements the same interface as the real object, so callers can use either without knowing the difference.

---

## The Problem It Solves

You need to work with an object that is expensive to create, lives remotely, needs access control, or should cache its results — but you don't want every caller to carry that complexity. A proxy sits in front of the real object and handles the concern transparently.

```csharp
// Without proxy — every caller manages the concern itself
var repo = new DocumentRepository();      // always created up-front (expensive)
if (!user.CanRead(id)) throw ...;        // access logic scattered everywhere
if (cache.Has(id)) return cache.Get(id); // caching logic repeated in every method
return repo.Load(id);
```

With a proxy, the same `IDocumentRepository` interface hides all of that:

```csharp
// With proxy — caller just calls Load(); the proxy handles the rest
IDocumentRepository repo = BuildProxyStack(currentUser);
var doc = repo.Load("doc-1");  // auth, caching, lazy init — all invisible
```

---

## The Three Proxy Variants

This implementation shows the three most common variants:

| Variant | Class | What it controls |
|---------|-------|-----------------|
| **Virtual (Lazy)** | `LazyDocumentProxy` | Defers creation of the real object until the first call |
| **Caching** | `CachingDocumentProxy` | Caches results; skips the real object on repeated reads |
| **Protection** | `AuthorizationDocumentProxy` | Checks permissions; blocks calls that are not allowed |

All three implement `IDocumentRepository` — they are interchangeable and stackable.

---

## Domain: Document Service

| Role | Class | Description |
|------|-------|-------------|
| **Subject interface** | `IDocumentRepository` | `Load(id)` and `ListIds()` — what callers depend on |
| **Real subject** | `DocumentRepository` | Actual storage; outputs a log line on construction and each load |
| **Virtual proxy** | `LazyDocumentProxy` | Wraps a factory; creates `DocumentRepository` only on first use |
| **Caching proxy** | `CachingDocumentProxy` | Caches `Load` results; exposes `Invalidate` and `InvalidateAll` |
| **Protection proxy** | `AuthorizationDocumentProxy` | Filters `ListIds` and gates `Load` to an allow-list per user |

---

## Structure

```
IDocumentRepository
        ▲
        │ implements
        ├── DocumentRepository          (real subject)
        ├── LazyDocumentProxy           (virtual proxy)
        │       _inner: IDocumentRepository  (created on first call)
        ├── CachingDocumentProxy        (caching proxy)
        │       _inner: IDocumentRepository  (forwarded on cache miss)
        └── AuthorizationDocumentProxy  (protection proxy)
                _inner: IDocumentRepository  (forwarded only if allowed)
```

Because all four implement the same interface, they compose freely:

```csharp
IDocumentRepository repo =
    new AuthorizationDocumentProxy(       // outermost: check permission first
        new CachingDocumentProxy(         // middle: skip I/O if cached
            new LazyDocumentProxy()),     // innermost: create real repo on demand
        currentUser,
        allowedIds);
```

---

## Virtual (Lazy) Proxy

Useful when the real object is expensive to construct and may not always be needed (e.g., a database connection, a remote service client, a large file parser).

```csharp
public sealed class LazyDocumentProxy : IDocumentRepository
{
    private readonly Func<IDocumentRepository> _factory;
    private IDocumentRepository? _inner;

    public bool IsInitialized => _inner is not null;

    // _inner ??= _factory() — creates on first access, never again
    private IDocumentRepository Inner => _inner ??= _factory();

    public Document? Load(string id)       => Inner.Load(id);
    public IReadOnlyList<string> ListIds() => Inner.ListIds();
}
```

```
var proxy = new LazyDocumentProxy();
// IsInitialized == false — nothing allocated yet

proxy.Load("doc-1");
// IsInitialized == true  — real repo created exactly once
```

---

## Caching Proxy

Useful when the real object involves I/O (database, HTTP, disk) and the same result is likely to be requested more than once.

```csharp
public Document? Load(string id)
{
    if (_cache.TryGetValue(id, out var cached))
        return cached;               // cache hit — no I/O

    var doc = _inner.Load(id);       // cache miss — forward to real object
    _cache[id] = doc;                // store null results too
    return doc;
}
```

Note that `null` results are also cached. If the first load of an id returns `null`, the proxy stores `null` and subsequent calls return it without ever calling the inner object again. This prevents repeated "miss" lookups for non-existent ids.

---

## Authorization (Protection) Proxy

Useful when different callers should see different subsets of data, without the real object knowing anything about users or roles.

```csharp
public Document? Load(string id)
{
    if (!_allowedIds.Contains(id))
        throw new UnauthorizedAccessException(...);  // blocked before inner is called

    return _inner.Load(id);
}

public IReadOnlyList<string> ListIds()
    => _inner.ListIds().Where(_allowedIds.Contains).ToList();  // filtered view
```

The real `DocumentRepository` is completely unaware of users or permissions. The proxy holds the allow-list per user and enforces it.

---

## When to Use

- **Virtual proxy**: the real object is expensive to create and may not be needed at all (lazy initialisation, on-demand connection)
- **Caching proxy**: the same data is read repeatedly and the source is slow (remote API, DB query, disk)
- **Protection proxy**: different callers should have different access to the same underlying object

## When NOT to Use

- The real object is cheap and always needed — the proxy adds indirection with no benefit
- Access control belongs in the real object or a dedicated auth layer, not scattered across proxies
- You need the proxy to *add behaviour* (not just control access) — that's Decorator

---

## Benefits

- Transparent to callers — they depend only on the interface
- Concerns (laziness, caching, auth) stay in one place and don't leak into business code
- Stackable — each proxy has one job and delegates everything else

## Drawbacks

- Adds indirection; call chains can be long and hard to debug
- Stale caches require explicit invalidation
- Protection proxies must be set up correctly for every user — incorrect allow-lists are a security bug

---

## Running the Demo

```bash
cd src/2-Structural/2.7-Proxy/ProxyPattern
dotnet run
```

## Running Tests

```bash
cd src/2-Structural/2.7-Proxy/ProxyPattern.Tests
dotnet test
```

---

## Related Patterns

- **Decorator** — structurally identical to Proxy (both wrap the same interface); see below
- **Facade** — simplifies a complex subsystem; Proxy controls access to *one* object
- **Adapter** — changes the interface of an object; Proxy keeps the same interface

---

### Proxy vs Decorator

This is the most common point of confusion. The two patterns are structurally **identical** — both hold a reference to an object that implements a shared interface and forward calls to it. The difference is **intent and lifecycle**:

| | Proxy | Decorator |
|---|---|---|
| **Intent** | Control *access* to the real object | Add *behaviour* to the real object |
| **Who creates the inner object** | Often the proxy itself (virtual proxy) | Caller assembles the chain |
| **Does it own the inner object** | Often yes — lifecycle management is the point | No — it receives an existing object |
| **Structural difference** | None | None |

```csharp
// Proxy — creates and owns the real object; caller doesn't build the chain
IDocumentRepository repo = new LazyDocumentProxy();  // inner repo created internally on demand

// Decorator — caller assembles; decorator extends existing object's behaviour
INotifier notifier = new LoggingDecorator(new SmsDecorator(new ConsoleNotifier()));
```

In short: if you're restricting or mediating access (and often managing the real object's lifecycle), it's a proxy. If you're adding capabilities to an object the caller already has, it's a decorator.
