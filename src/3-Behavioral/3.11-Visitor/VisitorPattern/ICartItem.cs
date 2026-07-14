namespace VisitorPattern;

public interface ICartItem
{
    string  Name  { get; }
    decimal Price { get; }
    void Accept(ICartVisitor visitor);
}
