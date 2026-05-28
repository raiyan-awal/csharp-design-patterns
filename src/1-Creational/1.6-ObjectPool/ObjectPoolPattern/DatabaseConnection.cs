namespace ObjectPoolPattern;

/// <summary>
/// The POOLED OBJECT — a simulated database connection.
///
/// Key design decisions:
///
/// 1. IDisposable — the <c>using</c> statement automatically returns the
///    connection to the pool when the block exits. The caller never calls
///    Return() directly; they just let the using block end.
///
/// 2. Internal constructor — only <see cref="DatabaseConnectionPool"/> can
///    create connections. Callers must go through the pool.
///
/// 3. IsCheckedOut flag — prevents using a connection after it has been
///    returned to the pool (catch common misuse bugs at runtime).
///
/// 4. Configurable creation delay — lets the demo simulate the real cost of
///    opening a TCP connection, while tests can set the delay to 0 for speed.
/// </summary>
public sealed class DatabaseConnection : IDisposable
{
    private readonly DatabaseConnectionPool _pool;
    private bool _checkedOut = true; // starts true: newly created = in use

    // ── Identity ─────────────────────────────────────────────────────────────

    /// <summary>Unique identifier assigned when the physical connection is opened.</summary>
    public string ConnectionId { get; }

    /// <summary>Connection string this connection was opened against.</summary>
    public string ConnectionString { get; }

    /// <summary>How many times this physical connection has been reused from the pool.</summary>
    public int ReuseCount { get; private set; } = 0;

    /// <summary>Number of queries executed in the current checkout session.</summary>
    public int QueriesExecuted { get; private set; } = 0;

    /// <summary>True while the connection is held by a caller; false while idle in the pool.</summary>
    public bool IsCheckedOut => _checkedOut;

    // ── Construction (expensive) ──────────────────────────────────────────────

    internal DatabaseConnection(string connectionString, DatabaseConnectionPool pool, int creationDelayMs)
    {
        _pool            = pool;
        ConnectionString = connectionString;
        ConnectionId     = $"CONN-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        // Simulate the real cost of establishing a DB connection:
        // TCP handshake, TLS negotiation, authentication, session setup.
        if (creationDelayMs > 0)
            Thread.Sleep(creationDelayMs);
    }

    // ── Usage ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Simulates executing a SQL query on this connection.
    /// Throws if the connection has already been returned to the pool.
    /// </summary>
    public string ExecuteQuery(string sql)
    {
        if (!_checkedOut)
            throw new InvalidOperationException(
                $"Connection {ConnectionId} has been returned to the pool. " +
                "Acquire a new connection before executing queries.");

        QueriesExecuted++;
        return $"[{ConnectionId}] '{sql}' → row set #{QueriesExecuted}";
    }

    // ── Pool lifecycle (called by the pool, not by callers) ───────────────────

    /// <summary>Called by the pool when this connection is checked out for reuse.</summary>
    internal void OnAcquired()
    {
        _checkedOut     = true;
        QueriesExecuted = 0;
        ReuseCount++;
    }

    /// <summary>Called by the pool after the connection is returned and reset.</summary>
    internal void OnReturned()
    {
        _checkedOut     = false;
        QueriesExecuted = 0;
    }

    // ── IDisposable — the key pattern ─────────────────────────────────────────

    /// <summary>
    /// Returns the connection to the pool rather than destroying it.
    /// Safe to call multiple times — subsequent calls are no-ops.
    ///
    /// This enables the idiomatic <c>using</c> pattern:
    /// <code>
    /// using var conn = pool.Acquire();
    /// conn.ExecuteQuery("SELECT ...");
    /// // conn is returned to pool here automatically
    /// </code>
    /// </summary>
    public void Dispose()
    {
        if (!_checkedOut) return; // already returned — no-op
        _pool.Return(this);
    }
}
