namespace VisitorPattern;

public sealed class PhysicalProduct : ICartItem
{
    public string  Name     { get; }
    public decimal Price    { get; }
    public double  WeightKg { get; }

    public PhysicalProduct(string name, decimal price, double weightKg)
    {
        Name = name; Price = price; WeightKg = weightKg;
    }

    public void Accept(ICartVisitor visitor) => visitor.Visit(this);
}
