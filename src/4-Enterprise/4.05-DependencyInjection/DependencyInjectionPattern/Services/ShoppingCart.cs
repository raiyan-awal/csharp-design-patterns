namespace DependencyInjectionPattern;

// Registered as Scoped — stateful per checkout session.
// Each scope (one customer session) gets its own ShoppingCart instance.
public sealed class ShoppingCart : IShoppingCart
{
    public Guid InstanceId { get; } = Guid.NewGuid();

    private readonly List<CartItem> _items = [];

    public IReadOnlyList<CartItem> Items    => _items;
    public decimal                 Subtotal => _items.Sum(i => i.Subtotal);

    public void Add(Product product, int quantity = 1)
    {
        var existing = _items.FirstOrDefault(i => i.Product.Id == product.Id);
        if (existing is not null)
        {
            _items.Remove(existing);
            _items.Add(existing with { Quantity = existing.Quantity + quantity });
        }
        else
        {
            _items.Add(new CartItem(product, quantity));
        }
    }

    public void Remove(int productId)
        => _items.RemoveAll(i => i.Product.Id == productId);

    public void Clear() => _items.Clear();
}
