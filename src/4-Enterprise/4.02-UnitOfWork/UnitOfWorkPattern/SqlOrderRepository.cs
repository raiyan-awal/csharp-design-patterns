using System.Data;
using Dapper;

namespace UnitOfWorkPattern;

public sealed class SqlOrderRepository(IDbConnection conn, IDbTransaction tx) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(int id)
    {
        var order = await conn.QuerySingleOrDefaultAsync<Order>(
            "SELECT * FROM Orders WHERE Id = @Id", new { Id = id }, tx);
        if (order is null) return null;

        var items = await conn.QueryAsync<OrderItem>(
            "SELECT ProductId, ProductName, Quantity, UnitPrice FROM OrderItems WHERE OrderId = @OrderId",
            new { OrderId = id }, tx);
        order.Items = [..items];
        return order;
    }

    public async Task AddAsync(Order order)
    {
        order.Id = await conn.ExecuteScalarAsync<int>("""
            INSERT INTO Orders (CustomerName, OrderDate, TotalAmount)
            VALUES (@CustomerName, @OrderDate, @TotalAmount);
            SELECT last_insert_rowid();
            """, order, tx);

        foreach (var item in order.Items)
        {
            await conn.ExecuteAsync("""
                INSERT INTO OrderItems (OrderId, ProductId, ProductName, Quantity, UnitPrice)
                VALUES (@OrderId, @ProductId, @ProductName, @Quantity, @UnitPrice)
                """, new { OrderId = order.Id, item.ProductId, item.ProductName, item.Quantity, item.UnitPrice }, tx);
        }
    }
}
