# Object Pool Pattern

## 📖 Pattern Category
**Creational Pattern**

## 🎯 Intent
Maintain a pool of reusable objects and hand them out on demand, avoiding the cost of creating and destroying them repeatedly.

## 🤔 Problem
Opening a database connection requires a TCP handshake, TLS negotiation, authentication, and session setup — typically 200–500 ms. If your application creates a new connection for every query, it spends more time connecting than querying.

The same problem applies to any object whose construction is expensive: thread creation, socket setup, large buffer allocation, parser initialisation, etc.

## ✅ Solution
1. Pre-create (or lazily create) a fixed number of reusable objects — the **pool**
2. Callers **acquire** an object from the pool rather than constructing one
3. When done, callers **return** the object to the pool (not destroy it)
4. If all objects are in use, new callers **wait** until one is returned (or time out)

The `IDisposable` pattern in .NET makes this seamless: `Dispose()` returns the object to the pool instead of destroying it, so callers can use `using` blocks and the return happens automatically.

## 🏗️ Structure

```
     DatabaseConnectionPool
  ┌──────────────────────────────────────┐
  │ - _idle: ConcurrentQueue<Connection> │
  │ - _semaphore: SemaphoreSlim          │  ← enforces MaxPoolSize
  │ - _totalCreated: int                 │
  │                                      │
  │ + Acquire(timeoutMs) → Connection    │  ← blocks if pool exhausted
  │ + Return(connection)  [internal]     │  ← called by Connection.Dispose()
  │ + TotalCreated: int                  │
  │ + IdleCount: int                     │
  │ + MaxPoolSize: int                   │
  └──────────────────────────────────────┘
              │ creates / manages
              ▼
     DatabaseConnection  : IDisposable
  ┌──────────────────────────────────────┐
  │ + ConnectionId: string               │  ← unique ID of the physical connection
  │ + IsCheckedOut: bool                 │
  │ + ReuseCount: int                    │  ← how many times this object was reused
  │ + QueriesExecuted: int               │  ← resets to 0 on each return
  │                                      │
  │ + ExecuteQuery(sql) → string         │
  │ + Dispose()                          │  ← returns to pool, NOT destroy
  │                                      │
  │   [internal] OnAcquired()            │
  │   [internal] OnReturned()            │
  └──────────────────────────────────────┘
```

## 💻 Implementation in This Example

### Files:
- **DatabaseConnection.cs** — Pooled object; `IDisposable.Dispose()` returns to pool instead of destroying
- **DatabaseConnectionPool.cs** — The pool; `ConcurrentQueue` for idle connections, `SemaphoreSlim` for size enforcement
- **Program.cs** — Demo with 5 demonstrations + Pause() between each

### Key Implementation Points:

**1. `IDisposable` as the return mechanism**
```csharp
public void Dispose()
{
    if (!_checkedOut) return;   // already returned — safe no-op
    _pool.Return(this);         // return, not destroy
}

// Caller code — clean and automatic:
using var conn = pool.Acquire();
conn.ExecuteQuery("SELECT ...");
// conn returned to pool here — even if an exception was thrown
```

**2. `SemaphoreSlim` enforces the pool ceiling**
```csharp
// Acquire: take one permit (blocks if pool is full)
if (!_semaphore.Wait(timeoutMs))
    throw new TimeoutException("Pool exhausted...");

// Return: give the permit back
_semaphore.Release();
```

**3. `ConcurrentQueue` for thread-safe idle management**
```csharp
// Fast path — reuse an idle connection
if (_idle.TryDequeue(out var existing))
{
    existing.OnAcquired();
    return existing;
}

// Slow path — create a new one (pays the creation cost once)
Interlocked.Increment(ref _totalCreated);
return new DatabaseConnection(_connectionString, this, _creationDelayMs);
```

**4. Reset on return — clean slate for the next caller**
```csharp
internal void OnReturned()
{
    _checkedOut     = false;
    QueriesExecuted = 0;   // reset session-specific state
}
```

## 🔍 Pool vs No Pool

| | Without Pool | With Pool |
|---|---|---|
| **Connection #1** | Create (200 ms) | Create (200 ms) |
| **Connection #2** | Create (200 ms) | Reuse (< 1 ms) |
| **Connection #3** | Create (200 ms) | Reuse (< 1 ms) |
| **100 operations** | 100 × 200 ms = 20 s | 200 ms + 99 × ~0 ms |
| **TotalCreated** | 100 | 1–N (N = pool size) |

## 🚀 How to Run

```bash
cd src/1-Creational/1.6-ObjectPool/ObjectPoolPattern
dotnet run
```

## 🧪 Running Tests

```bash
cd src/1-Creational/1.6-ObjectPool/ObjectPoolPattern.Tests
dotnet test
```

## 🧪 What the Demo Shows

1. **The cost problem** — 5 operations × 150 ms creation = slow; all time spent connecting
2. **Basic pool usage** — Round 1 pays the creation cost; Round 2 reuses instantly
3. **IDisposable / using** — automatic return; same `ConnectionId` appears in every iteration
4. **Reuse in depth** — pool of size 1 serves 5 operations; `ReuseCount` climbs to 4
5. **Pool exhaustion** — `TimeoutException` when all connections are held; success after one is returned

## ✅ Benefits

| Benefit | Description |
|---------|-------------|
| **Eliminates repeated creation cost** | Pay the setup cost once; amortise it across hundreds of uses |
| **Bounds resource usage** | `MaxPoolSize` prevents unbounded connection/thread/memory growth |
| **Transparent to callers** | `using var conn = pool.Acquire()` looks like normal object usage |
| **Thread-safe by design** | `ConcurrentQueue` + `SemaphoreSlim` handle concurrent callers without external locks |

## ❌ Drawbacks

| Drawback | Description |
|----------|-------------|
| **Stale state** | If `Reset()` is incomplete, one caller's state leaks into the next (bugs are subtle) |
| **Sizing is hard** | Too small → contention and timeouts; too large → wasted resources |
| **Lifetime complexity** | Pool must outlive all callers; disposing the pool while connections are checked out is dangerous |

## 🎓 When to Use

✅ **Good Candidates:**
- Database connections (the canonical example)
- HTTP connections / sockets
- Thread management (OS threads are expensive to create)
- Large buffer/array allocation in high-throughput code
- Parsers, compilers, or other stateful objects with expensive initialisation

❌ **Bad Candidates:**
- Cheap objects (plain POCOs, small value types) — pooling overhead exceeds the benefit
- Objects that hold unresettable state — if you can't clean them properly, don't pool them
- Objects where the pool would hold one item — just keep a field

## 🔀 Alternatives

| Alternative | When to Use Instead |
|-------------|---------------------|
| **Prototype (1.5)** | Cost is in copying state, not in physical resource acquisition |
| **`ArrayPool<T>`** | Pooling arrays/buffers specifically — already built into .NET |
| **`Microsoft.Extensions.ObjectPool`** | Generic pool for any type, already tested and production-ready |
| **ADO.NET connection pooling** | For database connections specifically — it's built in and on by default |

## 📚 Related Patterns

- **Prototype (1.5)** — Prototype clones state; Object Pool reuses the same object instance
- **Singleton (1.1)** — Singleton has exactly one instance; pool has N bounded instances
- **Flyweight (2.6)** — Flyweight shares immutable objects; Object Pool shares mutable objects that are reset between uses

## 🔑 Key Takeaways

1. **`Dispose()` returns, not destroys** — the caller's `using` block is the return mechanism
2. **`SemaphoreSlim` is the ceiling** — it blocks callers when the pool is full, rather than over-allocating
3. **`Reset()` is critical** — incomplete reset leaks one caller's state into the next
4. **`creationDelayMs` is injectable** — tests set it to 0 for speed; the demo uses 150 ms to show the real cost
5. **In .NET you rarely build this yourself** — `SqlConnection` pooling, `IHttpClientFactory`, and `ArrayPool<T>` are already production-grade implementations of this pattern

## 📖 Further Reading

- "Design Patterns: Elements of Reusable Object-Oriented Software" (Gang of Four) — Chapter 3
- Microsoft Docs: [Connection Pooling (ADO.NET)](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/connection-pooling)
- Microsoft Docs: [Object reuse with ObjectPool](https://learn.microsoft.com/en-us/aspnet/core/performance/objectpool)

---

← **Previous Pattern:** [1.5 - Prototype](../1.5-Prototype/)
→ **Next Pattern:** [2.1 - Adapter](../../2-Structural/2.1-Adapter/)
