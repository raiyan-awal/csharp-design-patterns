namespace EventSourcingPattern.Domain.Events;

public sealed record PointsEarnedEvent(
    int      MemberId,
    int      Amount,
    string   Reason,
    DateTime OccurredAt) : IDomainEvent;
