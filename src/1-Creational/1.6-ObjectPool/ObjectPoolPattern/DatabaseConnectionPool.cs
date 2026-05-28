using System.Collections.Concurrent;

namespace ObjectPoolPattern;

/// <summary>
/// The POOL — manages a bounded set of reusable <see cref="DatabaseConnection"/> objects.
///
/// Internals:
///   • <see cref="ConcurrentQueue{T}"/>  — holds idle connections; thread-safe enqueue/dequeue
///   • <see cref="SemaphoreSlim"/>       — enforces the MaxPoolSize ceiling and implements
///                                          wait-with-timeout when the pool is exhausted
///
/// Lifecycle of a connection:
///   1. Caller calls <see cref="Acquire"/>
///   2. Pool checks the idle queue — reuses if available, creates new if not
///   3. Caller uses the connection
///   4. Caller disposes (or calls the using block end) → <see cref="Return"/> is invoked
///   5. Connection is reset and enqueued back as idle
///
/// Thread safety:
///   ConcurrentQueue and SemaphoreSlim are both thread-safe, so multiple threads
///   can Acquire/Return simultaneously without external locking.
/// </summary>
public sealed class DatabaseConnectionPool : IDisposable
{
    private readonly string _connectionString;
    private readonly int    _maxPoolSize;
    private readonly int    _creationDelayMs;

    private readonly ConcurrentQueue<DatabaseConnection> _idle = new();
    private readonly SemaphoreSlim _semaphore;

    private int  _totalCreated = 0;
    private bool _disposed     = false;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <param name="connectionString">Database connection string (passed to each connection).</param>
    /// <param name="maxPoolSize">Maximum number of connections that may exist simultaneously.</param>
    /// <param name="creationDelayMs">
    /// Milliseconds to sleep when creating a new connection — simulates the real
    /// cost of a TCP handshake + authentication. Set to 0 in tests for speed.
    /// </param>
    public DatabaseConnectionPool(
        string connectionString,
        int maxPoolSize      = 10,
        int creationDelayMs  = 200)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));
        if (maxPoolSize < 1)
            throw new ArgumentOutOfRangeException(nameof(maxPoolSize), "Pool size must be at least 1.");

        _connectionString = connectionString;
        _maxPoolSize      = maxPoolSize;
        _creationDelayMs  = creationDelayMs;

        // SemaphoreSlim(initialCount, maxCount):
        //   initialCount = how many permits are available right now
        //   maxCount     = the ceiling — permits can never exceed this value
        // Each Acquire() takes one permit; each Return() gives one back.
        _semaphore = new SemaphoreSlim(maxPoolSize, maxPoolSize);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Acquires a connection from the pool.
    ///
    /// If an idle connection is available it is reused immediately (fast path).
    /// If no idle connection exists but the pool is not full, a new one is created.
    /// If the pool is full and all connections are in use, this call BLOCKS until
    /// one is returned or <paramref name="timeoutMs"/> elapses.
    /// </summary>
    /// <param name="timeoutMs">Max milliseconds to wait when pool is exhausted (default 3 s).</param>
    /// <exception cref="TimeoutException">Pool exhausted and timeout elapsed.</exception>
    /// <exception cref="ObjectDisposedException">Pool has been disposed.</exception>
    public DatabaseConnection Acquire(int timeoutMs = 3000)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Wait for a permit — blocks if all connections are checked out
        if (!_semaphore.Wait(timeoutMs))
            throw new TimeoutException(
                $"Could not acquire a connection within {timeoutMs} ms. " +
                $"All {_maxPoolSize} connections are currently in use. " +
                "Consider increasing MaxPoolSize or returning connections sooner.");

        // Fast path — reuse an idle connection
        if (_idle.TryDequeue(out var existing))
        {
            existing.OnAcquired();
            return existing;
        }

        // Slow path — create a brand-new connection (expensive)
        Interlocked.Increment(ref _totalCreated);
        return new DatabaseConnection(_connectionString, this, _creationDelayMs);
    }

    /// <summary>
    /// Returns a connection to the pool. Called automatically by
    /// <see cref="DatabaseConnection.Dispose"/>; callers should not invoke this directly.
    /// </summary>
    internal void Return(DatabaseConnection connection)
    {
        if (_disposed) return;

        connection.OnReturned();
        _idle.Enqueue(connection);
        _semaphore.Release(); // give the permit back
    }

    // ── Diagnostics ───────────────────────────────────────────────────────────

    /// <summary>Total physical connections ever opened (each one paid the creation cost).</summary>
    public int TotalCreated => _totalCreated;

    /// <summary>Connections currently idle in the pool, ready to be reused.</summary>
    public int IdleCount => _idle.Count;

    /// <summary>Connections currently checked out by callers.</summary>
    public int InUseCount => _maxPoolSize - _semaphore.CurrentCount - _idle.Count;

    /// <summary>The configured upper limit on simultaneous connections.</summary>
    public int MaxPoolSize => _maxPoolSize;

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _semaphore.Dispose();
    }
}
