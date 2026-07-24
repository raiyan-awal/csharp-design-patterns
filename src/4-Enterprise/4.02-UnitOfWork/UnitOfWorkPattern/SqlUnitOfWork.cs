using System.Data;
using Dapper;

namespace UnitOfWorkPattern;

// SQL Unit of Work: opens one IDbTransaction and hands the same connection
// and transaction to every repository it creates. CommitAsync commits
// the whole transaction; RollbackAsync (or Dispose without a Commit)
// rolls it all back — the database itself enforces the atomicity that
// InMemoryUnitOfWork has to simulate by hand.
//
// The connection belongs to the caller (e.g. one per HTTP request) —
// Dispose only ends the transaction, leaving the connection open so it
// can be reused across many Units of Work, one per business transaction.
public sealed class SqlUnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _conn;
    private readonly IDbTransaction _tx;
    private bool _completed;

    public SqlUnitOfWork(IDbConnection connection)
    {
        _conn = connection;
        if (_conn.State != ConnectionState.Open)
            _conn.Open();
        InitSchema();
        _tx = _conn.BeginTransaction();

        Products = new SqlProductRepository(_conn, _tx);
        Orders   = new SqlOrderRepository(_conn, _tx);
    }

    public IProductRepository Products { get; }
    public IOrderRepository   Orders   { get; }

    private void InitSchema()
    {
        _conn.Execute("""
            CREATE TABLE IF NOT EXISTS Products (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                Name          TEXT    NOT NULL,
                Price         REAL    NOT NULL,
                StockQuantity INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS Orders (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                CustomerName TEXT    NOT NULL,
                OrderDate    TEXT    NOT NULL,
                TotalAmount  REAL    NOT NULL
            );
            CREATE TABLE IF NOT EXISTS OrderItems (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderId     INTEGER NOT NULL,
                ProductId   INTEGER NOT NULL,
                ProductName TEXT    NOT NULL,
                Quantity    INTEGER NOT NULL,
                UnitPrice   REAL    NOT NULL
            );
            """);
    }

    public Task CommitAsync()
    {
        _tx.Commit();
        _completed = true;
        Console.WriteLine("  [SQL UoW] Transaction committed");
        return Task.CompletedTask;
    }

    public Task RollbackAsync()
    {
        _tx.Rollback();
        _completed = true;
        Console.WriteLine("  [SQL UoW] Transaction rolled back");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_completed)
        {
            _tx.Rollback();
            Console.WriteLine("  [SQL UoW] Disposed without Commit — transaction rolled back");
        }
        _tx.Dispose();
    }

    // Demo/test convenience — creates the schema and seeds products outside
    // of any Unit of Work transaction, the same way a migration and seed
    // script would run once before the application starts issuing business
    // transactions.
    public static void SeedCanadian(IDbConnection conn)
    {
        if (conn.State != ConnectionState.Open) conn.Open();
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS Products (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                Name          TEXT    NOT NULL,
                Price         REAL    NOT NULL,
                StockQuantity INTEGER NOT NULL DEFAULT 0
            );
            """);
        conn.Execute("""
            INSERT INTO Products (Name, Price, StockQuantity) VALUES
                ('Roots Cabin Hoodie',   89.99, 25),
                ('Canada Goose Toque',   45.00, 50),
                ('Muskoka Cast Iron Pan',64.99,  3),
                ('Blundstone 550 Boots',219.99, 10);
            """);
    }
}
