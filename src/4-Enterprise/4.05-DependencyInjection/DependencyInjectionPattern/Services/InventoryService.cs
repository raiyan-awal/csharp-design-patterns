namespace DependencyInjectionPattern;

// Registered as Singleton — expensive to initialise (simulates a DB load),
// so it is created once and shared across all consumers for the container's lifetime.
public sealed class InventoryService : IInventoryService
{
    public Guid InstanceId { get; } = Guid.NewGuid();

    private readonly Dictionary<int, (Product Product, int Stock)> _inventory;

    public InventoryService()
    {
        // Simulate an expensive one-time load from a database or external catalogue.
        _inventory = new()
        {
            [1] = (new(1, "Roam Portable Speaker",   "Electronics", 89.99m),  50),
            [2] = (new(2, "Trek Hiking Boots",        "Footwear",    229.99m), 30),
            [3] = (new(3, "Maple Leaf Tote Bag",      "Accessories", 34.99m),  100),
            [4] = (new(4, "North Coast Wool Sweater", "Clothing",    119.99m), 45),
            [5] = (new(5, "Summit Water Bottle",      "Outdoors",    44.99m),  75),
        };
    }

    public IReadOnlyList<Product> GetAll()
        => _inventory.Values.Select(e => e.Product).ToList();

    public Product? GetById(int id)
        => _inventory.TryGetValue(id, out var entry) ? entry.Product : null;

    public bool IsInStock(int productId, int quantity = 1)
        => _inventory.TryGetValue(productId, out var entry) && entry.Stock >= quantity;

    public bool Reserve(int productId, int quantity = 1)
    {
        if (!_inventory.TryGetValue(productId, out var entry) || entry.Stock < quantity)
            return false;

        _inventory[productId] = (entry.Product, entry.Stock - quantity);
        return true;
    }
}
