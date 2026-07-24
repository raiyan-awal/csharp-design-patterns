namespace UnitOfWorkPattern;

public sealed class OrderItem
{
    public int     ProductId   { get; set; }
    public string  ProductName { get; set; } = "";
    public int     Quantity    { get; set; }
    public decimal UnitPrice   { get; set; }

    public decimal LineTotal => Quantity * UnitPrice;

    public override string ToString() =>
        $"{Quantity} x {ProductName,-24} @ ${UnitPrice,8:F2} = ${LineTotal,9:F2}";
}
