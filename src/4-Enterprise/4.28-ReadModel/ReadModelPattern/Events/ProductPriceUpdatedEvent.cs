namespace ReadModelPattern.Events;

public sealed record ProductPriceUpdatedEvent(
    string ProductId,
    decimal NewPriceCAD,
    DateTimeOffset OccurredAt) : IDomainEvent;
