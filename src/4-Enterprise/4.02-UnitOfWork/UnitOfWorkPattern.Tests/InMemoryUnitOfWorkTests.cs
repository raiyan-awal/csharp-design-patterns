using UnitOfWorkPattern;

namespace UnitOfWorkPattern.Tests;

public class InMemoryUnitOfWorkTests
{
    private static InMemoryDataStore SeedStore() => InMemoryDataStore.SeedCanadian();

    [Fact]
    public async Task PlaceOrderAsync_SufficientStock_CommitsOrderAndDecrementsStock()
    {
        var store = SeedStore();
        using var uow = new InMemoryUnitOfWork(store);

        var order = await OrderService.PlaceOrderAsync(uow, "Priya", [new CartLine(1, 2), new CartLine(2, 1)]);

        Assert.True(order.Id > 0);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(89.99m * 2 + 45.00m, order.TotalAmount);

        var hoodie = store.Products.Single(p => p.Id == 1);
        var toque  = store.Products.Single(p => p.Id == 2);
        Assert.Equal(23, hoodie.StockQuantity);
        Assert.Equal(49, toque.StockQuantity);
        Assert.Single(store.Orders);
    }

    [Fact]
    public async Task PlaceOrderAsync_InsufficientStock_ThrowsAndLeavesStoreUntouched()
    {
        var store = SeedStore();
        using var uow = new InMemoryUnitOfWork(store);

        // Muskoka Cast Iron Pan (Id 3) only has 3 in stock.
        var act = () => OrderService.PlaceOrderAsync(uow, "Jordan", [new CartLine(1, 1), new CartLine(3, 10)]);

        await Assert.ThrowsAsync<InvalidOperationException>(act);

        // Item 1 (hoodie) was staged before the failure on item 2 (pan) —
        // it must NOT have been applied to the store, since CommitAsync
        // was never called.
        var hoodie = store.Products.Single(p => p.Id == 1);
        Assert.Equal(25, hoodie.StockQuantity);
        Assert.Empty(store.Orders);
    }

    [Fact]
    public async Task RollbackAsync_DiscardsStagedChanges()
    {
        var store = SeedStore();
        using var uow = new InMemoryUnitOfWork(store);

        var product = await uow.Products.GetByIdAsync(1);
        product!.StockQuantity = 0;
        await uow.Products.UpdateAsync(product);

        await uow.RollbackAsync();

        var afterRollback = store.Products.Single(p => p.Id == 1);
        Assert.Equal(25, afterRollback.StockQuantity);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsClone_NotLiveReferenceIntoStore()
    {
        var store = SeedStore();
        using var uow = new InMemoryUnitOfWork(store);

        var product = await uow.Products.GetByIdAsync(1);
        product!.StockQuantity = 999; // mutate the returned copy only

        var stillInStore = store.Products.Single(p => p.Id == 1);
        Assert.Equal(25, stillInStore.StockQuantity);
    }

    [Fact]
    public async Task SecondUnitOfWork_SeesChangesCommittedByFirst()
    {
        var store = SeedStore();

        using (var first = new InMemoryUnitOfWork(store))
            await OrderService.PlaceOrderAsync(first, "Amara", [new CartLine(4, 1)]);

        using var second = new InMemoryUnitOfWork(store);
        var boots = await second.Products.GetByIdAsync(4);
        Assert.Equal(9, boots!.StockQuantity);
    }
}
