namespace EventSourcingPattern.Domain.Events;

public sealed record PointsRedeemedEvent(
    int      MemberId,
    int      Amount,
    string   Reason,
    DateTime OccurredAt) : IDomainEvent;
