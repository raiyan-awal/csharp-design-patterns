namespace FacadePattern;

public sealed record OrderLine(string ProductId, string ProductName, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => Quantity * UnitPrice;
}
