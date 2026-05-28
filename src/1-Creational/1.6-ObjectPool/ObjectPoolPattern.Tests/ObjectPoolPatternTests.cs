using ObjectPoolPattern;

namespace ObjectPoolPattern.Tests;

/// <summary>
/// Unit tests for the Object Pool pattern implementation.
///
/// All tests use creationDelayMs: 0 so the suite runs fast.
///
/// These tests verify that:
/// 1. New connections are created when the pool is empty
/// 2. Connections are reused after being returned (TotalCreated does not grow)
/// 3. Disposing a connection returns it to the pool (IdleCount increases)
/// 4. A returned connection's state is reset for the next caller
/// 5. Using a returned connection throws an exception
/// 6. Pool respects MaxPoolSize — blocks and times out when exhausted
/// 7. ReuseCount and QueriesExecuted track correctly
/// 8. Pool can serve concurrent callers without creating extra connections
/// </summary>
public class ObjectPoolPatternTests : IDisposable
{
    // Every test gets its own pool with no creation delay and a size of 3.
    private readonly DatabaseConnectionPool _pool =
        new("Server=test;Database=TestDb;", maxPoolSize: 3, creationDelayMs: 0);

    public void Dispose() => _pool.Dispose();

    // ─────────────────────────────────────────────────────────────────────────
    // CONNECTION CREATION
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Acquire_FromEmptyPool_CreatesNewConnection()
    {
        using var conn = _pool.Acquire();

        Assert.Equal(1, _pool.TotalCreated);
    }

    [Fact]
    public void Acquire_TwoConnections_CreatesTwoConnections()
    {
        using var c1 = _pool.Acquire();
        using var c2 = _pool.Acquire();

        Assert.Equal(2, _pool.TotalCreated);
    }

    [Fact]
    public void Acquire_NewConnection_IsCheckedOut()
    {
        using var conn = _pool.Acquire();

        Assert.True(conn.IsCheckedOut);
    }

    [Fact]
    public void Acquire_NewConnection_HasUniqueConnectionId()
    {
        using var c1 = _pool.Acquire();
        using var c2 = _pool.Acquire();

        Assert.NotEqual(c1.ConnectionId, c2.ConnectionId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CONNECTION REUSE — the core value of the pool
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Acquire_AfterReturn_ReusesExistingConnection()
    {
        // Acquire, note the ID, return
        var firstId = string.Empty;
        using (var conn = _pool.Acquire())
        {
            firstId = conn.ConnectionId;
        } // ← Dispose() returns to pool here

        // Acquire again — should get the same physical connection
        using var reused = _pool.Acquire();
        Assert.Equal(firstId, reused.ConnectionId);
    }

    [Fact]
    public void Acquire_AfterReturn_DoesNotCreateNewConnection()
    {
        using (_pool.Acquire()) { } // acquire and return

        Assert.Equal(1, _pool.TotalCreated); // still only 1 physical connection

        using (_pool.Acquire()) { } // reuse

        Assert.Equal(1, _pool.TotalCreated); // still 1 — no new connection created
    }

    [Fact]
    public void Acquire_AfterReturn_ReuseCountIncremented()
    {
        using (_pool.Acquire()) { } // first checkout (ReuseCount goes to 1 on re-acquire)

        using var conn = _pool.Acquire();
        Assert.Equal(1, conn.ReuseCount);
    }

    [Fact]
    public void Acquire_MultipleReuses_ReuseCountAccumulates()
    {
        for (int i = 0; i < 4; i++)
            using (_pool.Acquire()) { }

        using var conn = _pool.Acquire();
        Assert.Equal(4, conn.ReuseCount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POOL STATE TRACKING
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IdleCount_AfterReturn_Increases()
    {
        var conn = _pool.Acquire();
        Assert.Equal(0, _pool.IdleCount);

        conn.Dispose();
        Assert.Equal(1, _pool.IdleCount);
    }

    [Fact]
    public void IdleCount_AfterAcquire_Decreases()
    {
        // Put one connection in the pool
        _pool.Acquire().Dispose();
        Assert.Equal(1, _pool.IdleCount);

        // Take it out again
        using var conn = _pool.Acquire();
        Assert.Equal(0, _pool.IdleCount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CONNECTION RESET ON RETURN
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Return_ResetsQueriesExecuted()
    {
        using (var conn = _pool.Acquire())
        {
            conn.ExecuteQuery("SELECT 1");
            conn.ExecuteQuery("SELECT 2");
            Assert.Equal(2, conn.QueriesExecuted);
        } // returned here

        using var reused = _pool.Acquire();
        // QueriesExecuted must be reset to 0 for the new caller
        Assert.Equal(0, reused.QueriesExecuted);
    }

    [Fact]
    public void Return_SetsIsCheckedOut_False()
    {
        var conn = _pool.Acquire();
        conn.Dispose();

        Assert.False(conn.IsCheckedOut);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MISUSE PROTECTION
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ExecuteQuery_AfterReturn_ThrowsInvalidOperationException()
    {
        var conn = _pool.Acquire();
        conn.Dispose(); // return to pool

        // Using a returned connection must throw
        Assert.Throws<InvalidOperationException>(() => conn.ExecuteQuery("SELECT 1"));
    }

    [Fact]
    public void Dispose_CalledTwice_IsNoOp()
    {
        var conn = _pool.Acquire();
        conn.Dispose();

        // Second dispose must not throw and must not double-return to pool
        var idleCountAfterFirst = _pool.IdleCount;
        conn.Dispose();

        Assert.Equal(idleCountAfterFirst, _pool.IdleCount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POOL EXHAUSTION
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Acquire_WhenPoolExhausted_ThrowsTimeoutException()
    {
        using var tinyPool = new DatabaseConnectionPool("Server=test;", maxPoolSize: 2, creationDelayMs: 0);

        // Hold both connections without returning them
        var c1 = tinyPool.Acquire();
        var c2 = tinyPool.Acquire();

        // Third acquire must time out immediately (very short timeout)
        Assert.Throws<TimeoutException>(() => tinyPool.Acquire(timeoutMs: 50));

        c1.Dispose();
        c2.Dispose();
    }

    [Fact]
    public void Acquire_AfterExhaustedPoolReceivesReturn_Succeeds()
    {
        using var tinyPool = new DatabaseConnectionPool("Server=test;", maxPoolSize: 1, creationDelayMs: 0);

        var held = tinyPool.Acquire();

        // Release on a background thread after a short delay
        Task.Delay(100).ContinueWith(_ => held.Dispose());

        // This acquire should succeed once held is returned
        var exception = Record.Exception(() =>
        {
            using var conn = tinyPool.Acquire(timeoutMs: 1000);
        });

        Assert.Null(exception);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // QUERY EXECUTION
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ExecuteQuery_ReturnsNonEmptyResult()
    {
        using var conn = _pool.Acquire();
        var result = conn.ExecuteQuery("SELECT * FROM Orders");

        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void ExecuteQuery_IncrementsQueriesExecuted()
    {
        using var conn = _pool.Acquire();

        conn.ExecuteQuery("SELECT 1");
        conn.ExecuteQuery("SELECT 2");
        conn.ExecuteQuery("SELECT 3");

        Assert.Equal(3, conn.QueriesExecuted);
    }

    [Fact]
    public void ExecuteQuery_ResultContainsConnectionId()
    {
        using var conn = _pool.Acquire();
        var result = conn.ExecuteQuery("SELECT 1");

        Assert.Contains(conn.ConnectionId, result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POOL CONFIGURATION
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Pool_MaxPoolSize_ReflectsConfiguration()
    {
        using var pool = new DatabaseConnectionPool("Server=test;", maxPoolSize: 5, creationDelayMs: 0);

        Assert.Equal(5, pool.MaxPoolSize);
    }

    [Fact]
    public void Pool_InvalidMaxPoolSize_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DatabaseConnectionPool("Server=test;", maxPoolSize: 0));
    }

    [Fact]
    public void Pool_EmptyConnectionString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new DatabaseConnectionPool("", maxPoolSize: 5));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CONCURRENT ACCESS
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Pool_ConcurrentAcquireAndReturn_NeverExceedsTotalCreated()
    {
        using var pool = new DatabaseConnectionPool("Server=test;", maxPoolSize: 3, creationDelayMs: 0);

        // Run 20 operations concurrently, each acquiring and immediately returning
        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
        {
            using var conn = pool.Acquire(timeoutMs: 5000);
            conn.ExecuteQuery("SELECT 1");
        })).ToArray();

        await Task.WhenAll(tasks);

        // With pool size 3, at most 3 physical connections should ever have been created
        Assert.True(pool.TotalCreated <= 3);
    }
}
