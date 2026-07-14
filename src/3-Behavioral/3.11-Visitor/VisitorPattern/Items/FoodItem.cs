namespace VisitorPattern;

public sealed class FoodItem : ICartItem
{
    public string  Name           { get; }
    public decimal Price          { get; }
    public bool    IsRefrigerated { get; }

    public FoodItem(string name, decimal price, bool isRefrigerated = false)
    {
        Name = name; Price = price; IsRefrigerated = isRefrigerated;
    }

    public void Accept(ICartVisitor visitor) => visitor.Visit(this);
}
