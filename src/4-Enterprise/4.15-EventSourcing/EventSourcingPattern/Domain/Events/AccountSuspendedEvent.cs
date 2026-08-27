namespace EventSourcingPattern.Domain.Events;

public sealed record AccountSuspendedEvent(
    int      MemberId,
    string   Reason,
    DateTime OccurredAt) : IDomainEvent;
