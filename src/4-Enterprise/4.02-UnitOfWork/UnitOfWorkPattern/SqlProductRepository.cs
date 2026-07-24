using System.Data;
using Dapper;

namespace UnitOfWorkPattern;

// Every call passes the shared transaction so its writes enlist in whatever
// SqlUnitOfWork.CommitAsync / RollbackAsync ultimately decides for the connection.
public sealed class SqlProductRepository(IDbConnection conn, IDbTransaction tx) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(int id) =>
        await conn.QuerySingleOrDefaultAsync<Product>(
            "SELECT * FROM Products WHERE Id = @Id", new { Id = id }, tx);

    public async Task UpdateAsync(Product product)
    {
        var rows = await conn.ExecuteAsync("""
            UPDATE Products
            SET Name = @Name, Price = @Price, StockQuantity = @StockQuantity
            WHERE Id = @Id
            """, product, tx);
        if (rows == 0) throw new InvalidOperationException($"Product #{product.Id} not found");
    }
}
