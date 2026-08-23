namespace DomainEventPattern.Events;

public sealed record AuctionOpenedEvent(
    int      AuctionId,
    string   Title,
    decimal  ReservePrice,
    DateTime OccurredAt) : IDomainEvent;
