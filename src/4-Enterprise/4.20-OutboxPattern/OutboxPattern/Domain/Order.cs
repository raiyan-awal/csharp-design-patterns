namespace OutboxPattern.Domain;

public sealed class Order
{
    public Guid            Id           { get; init; } = Guid.NewGuid();
    public string          CustomerId   { get; init; } = "";
    public string          CustomerName { get; init; } = "";
    public List<OrderItem> Items        { get; init; } = [];
    public decimal         TotalCAD     => Items.Sum(i => i.UnitPriceCAD * i.Quantity);
    public DateTime        PlacedAtUtc  { get; init; } = DateTime.UtcNow;
}
