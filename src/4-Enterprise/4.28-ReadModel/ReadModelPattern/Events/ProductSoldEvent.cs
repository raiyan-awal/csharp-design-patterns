namespace ReadModelPattern.Events;

public sealed record ProductSoldEvent(
    string ProductId,
    string SellerId,
    int Quantity,
    decimal PriceCAD,
    DateTimeOffset OccurredAt) : IDomainEvent;
