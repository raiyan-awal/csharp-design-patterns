namespace ObjectPoolPattern;

/// <summary>
/// OBJECT POOL PATTERN DEMONSTRATION
///
/// This program demonstrates the Object Pool pattern using database connections.
/// It shows:
/// 1. The cost problem — creating a new connection for every operation is expensive
/// 2. Basic pool usage — acquire, use, return manually
/// 3. IDisposable / using — automatic return when the block exits
/// 4. Connection reuse — the same physical connection serves multiple callers
/// 5. Pool exhaustion — what happens when all connections are in use
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== OBJECT POOL PATTERN DEMO ===");
        Console.WriteLine("Press any key after each section to continue.\n");

        const string connectionString = "Server=db.example.com;Database=Orders;User=app;";

        // ── DEMONSTRATION 1: The cost problem ────────────────────────────────
        Console.WriteLine("--- Demonstration 1: The Cost Problem ---");
        Console.WriteLine("Creating a brand-new connection for every operation.\n");
        Console.WriteLine("Each connection requires: TCP handshake → TLS negotiation → authentication → session setup");
        Console.WriteLine("In a real system this takes ~200-500 ms per connection.\n");
        Console.WriteLine("Simulating 5 operations WITHOUT a pool:");

        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 1; i <= 5; i++)
        {
            // No pool — create, use, destroy for every single operation
            var tempPool = new DatabaseConnectionPool(connectionString, maxPoolSize: 1, creationDelayMs: 150);
            using var conn = tempPool.Acquire();
            var result = conn.ExecuteQuery($"SELECT * FROM Orders WHERE id = {i}");
            Console.WriteLine($"  Op {i}: {result}  [new connection each time]");
            tempPool.Dispose();
        }

        sw.Stop();
        Console.WriteLine($"\n→ 5 operations, 5 connections created. Total time: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine("→ At 150 ms per connection, most of the time was spent just connecting.");

        Pause();

        // ── DEMONSTRATION 2: Basic pool usage ────────────────────────────────
        Console.WriteLine("--- Demonstration 2: Basic Pool Usage (Acquire → Use → Return) ---");
        Console.WriteLine("The pool creates connections once and reuses them.\n");

        var pool = new DatabaseConnectionPool(connectionString, maxPoolSize: 3, creationDelayMs: 150);

        Console.WriteLine("Round 1 — pool is empty, new connections must be created:");
        sw.Restart();

        var c1 = pool.Acquire();
        Console.WriteLine($"  Acquired {c1.ConnectionId}  [TotalCreated: {pool.TotalCreated}]");
        c1.ExecuteQuery("SELECT * FROM Products");

        var c2 = pool.Acquire();
        Console.WriteLine($"  Acquired {c2.ConnectionId}  [TotalCreated: {pool.TotalCreated}]");
        c2.ExecuteQuery("SELECT * FROM Customers");

        // Return both connections to the pool
        c1.Dispose();
        c2.Dispose();
        Console.WriteLine($"\n  Returned both. Idle in pool: {pool.IdleCount}");
        sw.Stop();
        Console.WriteLine($"  Round 1 time: {sw.ElapsedMilliseconds} ms (paid creation cost)");

        Console.WriteLine("\nRound 2 — connections are idle in the pool, reused instantly:");
        sw.Restart();

        var c3 = pool.Acquire();
        Console.WriteLine($"  Acquired {c3.ConnectionId}  (ReuseCount: {c3.ReuseCount}) [TotalCreated: {pool.TotalCreated}]");
        c3.ExecuteQuery("INSERT INTO Orders ...");

        var c4 = pool.Acquire();
        Console.WriteLine($"  Acquired {c4.ConnectionId}  (ReuseCount: {c4.ReuseCount}) [TotalCreated: {pool.TotalCreated}]");
        c4.ExecuteQuery("UPDATE Inventory ...");

        c3.Dispose();
        c4.Dispose();
        sw.Stop();
        Console.WriteLine($"  Round 2 time: {sw.ElapsedMilliseconds} ms (no creation cost — reused!)");

        Console.WriteLine($"\n→ TotalCreated stayed at {pool.TotalCreated} across both rounds.");
        Console.WriteLine("→ Same physical connections, zero reconnection overhead.");

        Pause();

        // ── DEMONSTRATION 3: IDisposable / using statement ───────────────────
        Console.WriteLine("--- Demonstration 3: Automatic Return via 'using' ---");
        Console.WriteLine("IDisposable lets the using block return the connection automatically.\n");

        Console.WriteLine("Without using (manual, error-prone):");
        Console.WriteLine("""
  var conn = pool.Acquire();
  try
  {
      conn.ExecuteQuery("SELECT ...");
  }
  finally
  {
      conn.Dispose(); // easy to forget, especially if an exception is thrown
  }
""");

        Console.WriteLine("With using (safe and idiomatic):");
        Console.WriteLine("""
  using var conn = pool.Acquire();
  conn.ExecuteQuery("SELECT ..."); // conn is returned to pool when this block exits
                                   // even if an exception is thrown
""");

        Console.WriteLine("Live demo — three sequential operations using three using blocks:");
        Console.WriteLine($"  Pool state before: Idle={pool.IdleCount}, TotalCreated={pool.TotalCreated}\n");

        for (int i = 1; i <= 3; i++)
        {
            using var conn = pool.Acquire();
            var result = conn.ExecuteQuery($"SELECT * FROM Orders WHERE batch = {i}");
            Console.WriteLine($"  [{i}] {result}  (ConnId: {conn.ConnectionId})");
        } // ← conn.Dispose() called here automatically each iteration

        Console.WriteLine($"\n  Pool state after: Idle={pool.IdleCount}, TotalCreated={pool.TotalCreated}");
        Console.WriteLine("→ All three operations used the same physical connection — created only once.");
        Console.WriteLine("→ Each using block returned it before the next one started.");

        Pause();

        // ── DEMONSTRATION 4: Connection reuse in depth ───────────────────────
        Console.WriteLine("--- Demonstration 4: Connection Reuse in Depth ---");
        Console.WriteLine("Showing ReuseCount to prove the same physical connection serves many callers.\n");

        // Force a fresh pool with one slot to make the reuse very obvious
        using var singlePool = new DatabaseConnectionPool(connectionString, maxPoolSize: 1, creationDelayMs: 0);

        Console.WriteLine("Running 5 operations through a pool of size 1:\n");

        for (int i = 1; i <= 5; i++)
        {
            using var conn = singlePool.Acquire();
            conn.ExecuteQuery($"Operation {i}");
            Console.WriteLine($"  Op {i}: ConnId={conn.ConnectionId}  ReuseCount={conn.ReuseCount}  QueriesInSession={conn.QueriesExecuted}");
        }

        Console.WriteLine($"\n  TotalCreated = {singlePool.TotalCreated} (one physical connection served all 5 operations)");
        Console.WriteLine("→ The ConnectionId is the same every time — it's the SAME object being reused.");
        Console.WriteLine("→ ReuseCount increments on each checkout; QueriesExecuted resets to 0 each time.");

        Pause();

        // ── DEMONSTRATION 5: Pool exhaustion ─────────────────────────────────
        Console.WriteLine("--- Demonstration 5: Pool Exhaustion ---");
        Console.WriteLine("What happens when all connections are in use and a new request comes in?\n");

        using var tinyPool = new DatabaseConnectionPool(connectionString, maxPoolSize: 2, creationDelayMs: 0);

        // Check out all connections without returning them
        var held1 = tinyPool.Acquire();
        var held2 = tinyPool.Acquire();
        Console.WriteLine($"Checked out all {tinyPool.MaxPoolSize} connections: {held1.ConnectionId}, {held2.ConnectionId}");
        Console.WriteLine($"Pool state: Idle={tinyPool.IdleCount}, InUse={tinyPool.InUseCount}\n");

        Console.WriteLine("Attempting to acquire a third connection (pool is full) with a 500 ms timeout:");
        try
        {
            var held3 = tinyPool.Acquire(timeoutMs: 500);
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"  ✓ TimeoutException caught:\n    {ex.Message}");
        }

        Console.WriteLine("\nReturning one connection and trying again:");
        held1.Dispose();
        Console.WriteLine($"  Returned {held1.ConnectionId}. Idle={tinyPool.IdleCount}");

        using var reacquired = tinyPool.Acquire(timeoutMs: 500);
        Console.WriteLine($"  ✓ Acquired {reacquired.ConnectionId} — same connection that was just returned.");

        held2.Dispose();

        Console.WriteLine("\n→ The pool blocks (not busy-waits) until a connection is freed or timeout elapses.");
        Console.WriteLine("→ This is safer than silently creating extra connections beyond the pool limit.");

        Pause();

        // ── SUMMARY ──────────────────────────────────────────────────────────
        Console.WriteLine("=== KEY TAKEAWAYS ===");
        Console.WriteLine("✓ Object Pool reuses expensive-to-create objects instead of constructing/destroying them");
        Console.WriteLine("✓ ConcurrentQueue holds idle objects; SemaphoreSlim enforces the size ceiling");
        Console.WriteLine("✓ IDisposable + using = automatic return — callers cannot forget to return");
        Console.WriteLine("✓ The pool blocks on exhaustion (with timeout) instead of silently over-allocating");
        Console.WriteLine("✓ TotalCreated vs operations-served shows the reuse multiplier");

        Console.WriteLine("\n=== REAL-WORLD USAGE IN .NET ===");
        Console.WriteLine("• ADO.NET connection pooling — built into SqlConnection, automatically managed");
        Console.WriteLine("• IHttpClientFactory — pools and reuses HttpMessageHandler instances");
        Console.WriteLine("• ArrayPool<T> — pools byte arrays to avoid GC pressure in high-throughput code");
        Console.WriteLine("• Microsoft.Extensions.ObjectPool — generic pool for any expensive object");
        Console.WriteLine("• Thread pool (ThreadPool) — reuses threads instead of creating/destroying them");
        Console.WriteLine("• ASP.NET Core: SocketsHttpHandler connection pooling for outbound HTTP");

        Console.WriteLine("\nDemo complete.");

        pool.Dispose();
    }

    static void Pause()
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey(intercept: true);
        Console.WriteLine();
    }
}
