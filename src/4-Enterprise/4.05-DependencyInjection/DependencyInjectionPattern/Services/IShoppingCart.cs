namespace DependencyInjectionPattern;

public interface IShoppingCart
{
    Guid                    InstanceId { get; }
    IReadOnlyList<CartItem> Items      { get; }
    decimal                 Subtotal   { get; }
    void Add(Product product, int quantity = 1);
    void Remove(int productId);
    void Clear();
}
