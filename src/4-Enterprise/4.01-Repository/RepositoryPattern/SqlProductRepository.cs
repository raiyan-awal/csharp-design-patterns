using System.Data;
using System.Linq.Expressions;
using Dapper;

namespace RepositoryPattern;

// SQL implementation of IRepository<Product> using Dapper.
// Accepts any ADO.NET IDbConnection — wire up SqliteConnection for this demo
// or SqlConnection (Microsoft.Data.SqlClient) in production:
//
//   new SqlProductRepository(new SqliteConnection("Data Source=:memory:"))
//   new SqlProductRepository(new SqlConnection("Server=...;Database=Shop;Trusted_Connection=true;"))
//
// The calling code is identical either way — only the constructor argument changes.
public sealed class SqlProductRepository : IRepository<Product>, IDisposable
{
    private readonly IDbConnection _conn;

    public SqlProductRepository(IDbConnection connection)
    {
        _conn = connection;
        if (_conn.State != ConnectionState.Open)
            _conn.Open();
        InitSchema();
    }

    private void InitSchema()
    {
        _conn.Execute("""
            CREATE TABLE IF NOT EXISTS Products (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                Name          TEXT    NOT NULL,
                Category      TEXT    NOT NULL,
                Price         REAL    NOT NULL,
                StockQuantity INTEGER NOT NULL DEFAULT 0,
                IsActive      INTEGER NOT NULL DEFAULT 1
            )
            """);
    }

    public async Task<Product?> GetByIdAsync(int id)
        => await _conn.QuerySingleOrDefaultAsync<Product>(
            "SELECT * FROM Products WHERE Id = @Id", new { Id = id });

    public async Task<IEnumerable<Product>> GetAllAsync()
        => await _conn.QueryAsync<Product>("SELECT * FROM Products");

    public async Task<IEnumerable<Product>> FindAsync(Expression<Func<Product, bool>> predicate)
    {
        // Dapper has no expression-to-SQL translator; load all rows and filter in memory.
        // With EF Core instead: _context.Products.Where(predicate).ToListAsync() — SQL generated.
        var all = await GetAllAsync();
        return all.Where(predicate.Compile());
    }

    public async Task AddAsync(Product entity)
    {
        entity.Id = await _conn.ExecuteScalarAsync<int>("""
            INSERT INTO Products (Name, Category, Price, StockQuantity, IsActive)
            VALUES (@Name, @Category, @Price, @StockQuantity, @IsActive);
            SELECT last_insert_rowid();
            """, entity);
        Console.WriteLine($"  [SQL]  Added    → {entity}");
    }

    public async Task UpdateAsync(Product entity)
    {
        var rows = await _conn.ExecuteAsync("""
            UPDATE Products
            SET Name = @Name, Category = @Category, Price = @Price,
                StockQuantity = @StockQuantity, IsActive = @IsActive
            WHERE Id = @Id
            """, entity);
        if (rows == 0) throw new InvalidOperationException($"Product #{entity.Id} not found");
        Console.WriteLine($"  [SQL]  Updated  → {entity}");
    }

    public async Task DeleteAsync(int id)
    {
        var rows = await _conn.ExecuteAsync(
            "DELETE FROM Products WHERE Id = @Id", new { Id = id });
        if (rows == 0) throw new InvalidOperationException($"Product #{id} not found");
        Console.WriteLine($"  [SQL]  Deleted  product #{id}");
    }

    public async Task<bool> ExistsAsync(int id)
        => await _conn.ExecuteScalarAsync<bool>(
            "SELECT COUNT(1) FROM Products WHERE Id = @Id", new { Id = id });

    public async Task<int> CountAsync()
        => await _conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products");

    public void Dispose() => _conn.Dispose();
}
