namespace OutboxPattern.Domain;

public sealed record OrderItem(string ProductName, int Quantity, decimal UnitPriceCAD);
