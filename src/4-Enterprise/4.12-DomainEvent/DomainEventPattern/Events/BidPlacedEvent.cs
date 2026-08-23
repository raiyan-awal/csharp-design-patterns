namespace DomainEventPattern.Events;

public sealed record BidPlacedEvent(
    int      AuctionId,
    string   Bidder,
    decimal  Amount,
    int      BidNumber,
    DateTime OccurredAt) : IDomainEvent;
