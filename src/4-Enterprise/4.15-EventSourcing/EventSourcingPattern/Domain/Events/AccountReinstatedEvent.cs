namespace EventSourcingPattern.Domain.Events;

public sealed record AccountReinstatedEvent(
    int      MemberId,
    DateTime OccurredAt) : IDomainEvent;
