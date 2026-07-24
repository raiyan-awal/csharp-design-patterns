namespace UnitOfWorkPattern;

// Stands in for "the database" — the durable state that only ever changes
// when an InMemoryUnitOfWork commits. Shared across UoW instances, so
// successive `using var uow = new InMemoryUnitOfWork(store)` blocks see each
// other's writes, the same way successive requests share one SQL database.
public sealed class InMemoryDataStore
{
    public readonly List<Product> Products = [];
    public readonly List<Order>   Orders   = [];
    public int NextOrderId = 1;
    public readonly Lock Gate = new();

    public static InMemoryDataStore SeedCanadian()
    {
        var store = new InMemoryDataStore();
        store.Products.AddRange(
        [
            new() { Id = 1, Name = "Roots Cabin Hoodie",     Price =  89.99m, StockQuantity = 25 },
            new() { Id = 2, Name = "Canada Goose Toque",      Price =  45.00m, StockQuantity = 50 },
            new() { Id = 3, Name = "Muskoka Cast Iron Pan",   Price =  64.99m, StockQuantity =  3 },
            new() { Id = 4, Name = "Blundstone 550 Boots",    Price = 219.99m, StockQuantity = 10 },
        ]);
        return store;
    }
}
