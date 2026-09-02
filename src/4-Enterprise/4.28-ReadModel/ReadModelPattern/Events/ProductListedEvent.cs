namespace ReadModelPattern.Events;

public sealed record ProductListedEvent(
    string ProductId,
    string SellerId,
    string Title,
    decimal PriceCAD,
    int InitialStock,
    DateTimeOffset OccurredAt) : IDomainEvent;
