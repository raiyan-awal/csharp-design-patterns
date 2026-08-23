namespace DomainEventPattern.Events;

public sealed record AuctionClosedEvent(
    int      AuctionId,
    string   Title,
    string?  Winner,
    decimal  WinningBid,
    bool     ReserveMet,
    DateTime OccurredAt) : IDomainEvent;
