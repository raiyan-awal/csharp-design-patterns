namespace DependencyInjectionPattern;

public sealed record CartItem(Product Product, int Quantity)
{
    public decimal Subtotal => Product.Price * Quantity;
}
