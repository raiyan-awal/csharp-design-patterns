namespace VisitorPattern;

public sealed class DigitalProduct : ICartItem
{
    public string  Name  { get; }
    public decimal Price { get; }

    public DigitalProduct(string name, decimal price)
    {
        Name = name; Price = price;
    }

    public void Accept(ICartVisitor visitor) => visitor.Visit(this);
}
