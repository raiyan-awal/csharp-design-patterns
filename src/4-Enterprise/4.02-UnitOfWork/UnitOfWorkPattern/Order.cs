namespace UnitOfWorkPattern;

public sealed class Order
{
    public int             Id           { get; set; }
    public string          CustomerName { get; set; } = "";
    public DateTime         OrderDate    { get; set; }
    public decimal          TotalAmount  { get; set; }
    public List<OrderItem> Items        { get; set; } = [];

    public override string ToString() =>
        $"Order #{Id} — {CustomerName}, {OrderDate:yyyy-MM-dd}, Total: ${TotalAmount:F2}, {Items.Count} item(s)";
}
