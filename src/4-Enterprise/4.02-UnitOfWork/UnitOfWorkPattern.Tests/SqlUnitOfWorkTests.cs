using Microsoft.Data.Sqlite;
using UnitOfWorkPattern;

namespace UnitOfWorkPattern.Tests;

public class SqlUnitOfWorkTests
{
    private static SqliteConnection SeedConnection()
    {
        var conn = new SqliteConnection("Data Source=:memory:");
        SqlUnitOfWork.SeedCanadian(conn);
        return conn;
    }

    [Fact]
    public async Task PlaceOrderAsync_SufficientStock_CommitsTransaction()
    {
        using var conn = SeedConnection();
        using var uow = new SqlUnitOfWork(conn);

        var order = await OrderService.PlaceOrderAsync(uow, "Priya", [new CartLine(1, 2)]);

        Assert.True(order.Id > 0);

        // Re-read through a fresh Unit of Work against the same connection —
        // the data is only visible because the first transaction actually
        // committed.
        using var verify = new SqlUnitOfWork(conn);
        var hoodie = await verify.Products.GetByIdAsync(1);
        Assert.Equal(23, hoodie!.StockQuantity);

        var persisted = await verify.Orders.GetByIdAsync(order.Id);
        Assert.NotNull(persisted);
        Assert.Single(persisted!.Items);
    }

    [Fact]
    public async Task PlaceOrderAsync_InsufficientStock_ThrowsAndTransactionNeverCommits()
    {
        using var conn = SeedConnection();
        using var uow = new SqlUnitOfWork(conn);

        var act = () => OrderService.PlaceOrderAsync(uow, "Jordan", [new CartLine(1, 1), new CartLine(3, 10)]);
        await Assert.ThrowsAsync<InvalidOperationException>(act);

        await uow.RollbackAsync();

        using var verify = new SqlUnitOfWork(conn);
        var hoodie = await verify.Products.GetByIdAsync(1);
        Assert.Equal(25, hoodie!.StockQuantity);
    }

    [Fact]
    public async Task DisposeWithoutCommit_RollsBackTransaction()
    {
        using var conn = SeedConnection();

        using (var uow = new SqlUnitOfWork(conn))
        {
            var product = await uow.Products.GetByIdAsync(2);
            product!.StockQuantity = 0;
            await uow.Products.UpdateAsync(product);
            // No CommitAsync — Dispose must roll the update back.
        }

        using var verify = new SqlUnitOfWork(conn);
        var toque = await verify.Products.GetByIdAsync(2);
        Assert.Equal(50, toque!.StockQuantity);
    }
}
