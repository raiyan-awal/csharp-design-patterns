# 1.6 — Object Pool

## Intent

Maintain a pool of reusable, expensive-to-create objects and lend them to callers on demand, returning them to the pool when the caller is done, so that the creation and teardown cost is paid only once per slot rather than once per request.

## The Problem It Solves

Without a pool, every operation that needs a connection pays the full creation cost on every call:

```csharp
// Without Object Pool: full creation and teardown on every query
public async Task<string> RunQuery(string sql)
{
    var conn = new DatabaseConnection(connectionString);
    // ← 200–500 ms for TCP handshake, TLS negotiation, and database authentication
    var result = await conn.ExecuteAsync(sql);
    conn.Close();
    // ← physical teardown discards the authenticated session immediately
    return result;
}
```

Problems with per-request creation:
- Expensive initialization (network, TLS, auth) is repeated on every call.
- Under load, the system spawns many short-lived objects, increasing GC pressure.
- No limit on concurrent connections — a burst of requests can exhaust the database server.
- Connection teardown discards a negotiated session that took hundreds of milliseconds to establish.

## Solution: Bounded pool with IDisposable return

`DatabaseConnectionPool` holds up to `maxPoolSize` pre-created connections in a `ConcurrentQueue<T>`. A `SemaphoreSlim` limits concurrent borrowers. When a caller's `using` block exits, `DatabaseConnection.Dispose()` returns the connection to the pool rather than destroying it.

```csharp
using var pool = new DatabaseConnectionPool(
    connectionString: "Server=db.toronto.ca;Database=MapleCatalogue",
    maxPoolSize: 5,
    creationDelayMs: 200);   // injectable delay for testing

// Borrow, use, return — all via 'using':
using (var conn = pool.Acquire())
{
    string result = conn.ExecuteQuery("SELECT * FROM Products");
}
// Dispose() returns conn to pool — no teardown, no re-authentication on the next borrow
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Pool | `DatabaseConnectionPool` | Creates connections up to `maxPoolSize`; serves them via `Acquire(int timeoutMs)`; `SemaphoreSlim` caps concurrent borrowers; `IDisposable` disposes the semaphore on shutdown |
| Pooled object | `DatabaseConnection` | Wraps a simulated database session; `IDisposable.Dispose()` returns `this` to the pool instead of tearing down; `ExecuteQuery(sql)` simulates a synchronous DB call |

## Structure

```
1.6-ObjectPool/
├── ObjectPoolPattern/
│   ├── DatabaseConnectionPool.cs   ← pool with ConcurrentQueue + SemaphoreSlim
│   ├── DatabaseConnection.cs       ← pooled object; DisposeAsync returns to pool
│   └── Program.cs
└── ObjectPoolPattern.Tests/
    └── ObjectPoolPatternTests.cs
```

## Key Code

### Pool with bounded concurrency

```csharp
public sealed class DatabaseConnectionPool : IDisposable
{
    private readonly ConcurrentQueue<DatabaseConnection> _idle = new();
    private readonly SemaphoreSlim _semaphore;

    public DatabaseConnectionPool(string connectionString,
        int maxPoolSize = 10, int creationDelayMs = 200)
    {
        _semaphore = new SemaphoreSlim(maxPoolSize, maxPoolSize);
        // ...
    }

    public DatabaseConnection Acquire(int timeoutMs = 3000)
    {
        if (!_semaphore.Wait(timeoutMs))          // blocks when pool is fully checked out
            throw new TimeoutException(
                $"Pool exhausted — all {MaxPoolSize} connections in use.");

        if (_idle.TryDequeue(out var conn))
        { conn.OnAcquired(); return conn; }        // fast path: reuse an idle connection

        return new DatabaseConnection(...);        // slow path: first-time creation only
    }

    internal void Return(DatabaseConnection conn)
    {
        conn.OnReturned();
        _idle.Enqueue(conn);
        _semaphore.Release();                      // allow the next waiter to proceed
    }
}
```

`SemaphoreSlim(maxPoolSize, maxPoolSize)` means at most `maxPoolSize` callers can hold a connection at the same time. Anyone beyond that blocks in `Wait(timeoutMs)` and receives a `TimeoutException` if the timeout elapses.

### IDisposable pattern — return, not destroy

```csharp
public sealed class DatabaseConnection : IDisposable
{
    public string ExecuteQuery(string sql)
    {
        QueriesExecuted++;
        return $"[{ConnectionId}] '{sql}' → row set #{QueriesExecuted}";
    }

    public void Dispose()
    {
        if (!_checkedOut) return;   // already returned — no-op
        _pool.Return(this);         // return to pool, not teardown
    }
}
```

The `using` pattern is the standard interaction. `Dispose()` returns the connection to the pool; if `Dispose()` is called twice it is a no-op because of the `_checkedOut` guard.

### Pool exhaustion

When all `maxPoolSize` connections are checked out, `Acquire` blocks in `SemaphoreSlim.Wait(timeoutMs)`. If the timeout elapses before any connection is returned, a `TimeoutException` is thrown — the caller never gets a connection but also never hangs indefinitely.

## Demo Scenarios

```
1. Creation cost comparison   — first acquire pays ~200 ms; subsequent acquires from the
                                idle queue are instant
2. Basic pool usage           — acquire a connection, run a query, return via await using
3. IDisposable / using        — confirms DisposeAsync returns to pool, not destroys
4. Reuse under load           — 10 sequential queries against a pool of 3 connections;
                                connections are reused, not recreated
5. Pool exhaustion            — maxSize=2, 3 concurrent acquires; third waits until one
                                of the first two is returned
```

## When to Use

- Object creation is expensive (network handshake, TLS, auth, thread startup) and the same logical object can be used by different callers at different times.
- The maximum number of concurrent instances must be bounded to protect a downstream resource (database connection limit, licensed service seats).
- The working set of the application requires many short-lived uses of the same object type.

## When NOT to Use

- When object creation is cheap — pooling adds synchronization overhead that exceeds the savings.
- When objects carry caller-specific state that makes reuse unsafe (e.g., a partially-read response stream).
- When the pool would almost never lend more than one object simultaneously — the machinery is wasted.
- In .NET, most connection pools are already built into the framework (`SqlConnection`, `HttpClient`) — building your own only makes sense for resources that are not already managed.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Amortized creation cost | The expensive initialization runs at most `maxSize` times, not once per request |
| Bounded resource use | `SemaphoreSlim` ensures the downstream resource (database) never receives more than `maxSize` concurrent connections |
| Reduced GC pressure | Long-lived pooled objects spend most of their time on the LOH, avoiding frequent gen-0 collections |
| Transparent to callers | `await using` handles return automatically; callers never interact with the pool directly |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Added complexity | A pool, a semaphore, and a modified `Dispose` are more moving parts than a plain `new` |
| Stale connections | A pooled connection can go stale while idle (server-side timeout, network reset); the pool must validate before lending |
| State leakage risk | If a connection carries session state (transactions, temp tables), returning it to the pool without cleanup corrupts the next borrower |
| Pool tuning | Under-sized pools cause excessive waiting; over-sized pools waste resources — the right size depends on load profiling |

## Related Patterns

- **Singleton (1.1)** — the pool itself is often a Singleton; the objects inside the pool are not.
- **Flyweight (2.6)** — both avoid repeated object creation; Flyweight objects are immutable and truly shared, while Pool objects are checked out exclusively to one caller at a time.
- **Proxy (2.7)** — a pooled object that intercepts `Dispose` to return itself to the pool rather than truly disposing is a form of Virtual Proxy.
- **Prototype (1.5)** — an alternative to pooling when cloning from a canonical prototype is cheaper than full initialization; unlike pooling, clones are disposable after use.

## Running the Demo

```bash
cd src/1-Creational/1.6-ObjectPool/ObjectPoolPattern
dotnet run
```

## Running the Tests

```bash
cd src/1-Creational/1.6-ObjectPool/ObjectPoolPattern.Tests
dotnet test
```
