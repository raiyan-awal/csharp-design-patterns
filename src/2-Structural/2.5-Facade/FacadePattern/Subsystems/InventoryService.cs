namespace FacadePattern;

// ============================================================
// SUBSYSTEM: Inventory
// ============================================================
// Manages product stock. Has no knowledge of payments,
// shipping, or any other subsystem.

public sealed class InventoryService
{
    private readonly Dictionary<string, int> _stock;

    public InventoryService(Dictionary<string, int>? initialStock = null)
        => _stock = initialStock is not null
            ? new Dictionary<string, int>(initialStock)
            : [];

    public bool IsAvailable(string productId, int quantity)
    {
        bool available = _stock.TryGetValue(productId, out var qty) && qty >= quantity;
        Console.WriteLine($"[INVENTORY] {productId}: {(available ? $"{qty} in stock — OK" : "out of stock")}");
        return available;
    }

    public void Reserve(string productId, int quantity)
    {
        if (!IsAvailable(productId, quantity))
            throw new InvalidOperationException($"Cannot reserve {quantity} of '{productId}'.");
        _stock[productId] -= quantity;
        Console.WriteLine($"[INVENTORY] Reserved {quantity}× {productId} (remaining: {_stock[productId]})");
    }

    public void Release(string productId, int quantity)
    {
        _stock.TryAdd(productId, 0);
        _stock[productId] += quantity;
        Console.WriteLine($"[INVENTORY] Released {quantity}× {productId} (now: {_stock[productId]})");
    }

    public int GetStock(string productId)
        => _stock.TryGetValue(productId, out var qty) ? qty : 0;
}
