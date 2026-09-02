namespace ReadModelPattern.Events;

public sealed record ReviewPostedEvent(
    string ProductId,
    int Rating,
    DateTimeOffset OccurredAt) : IDomainEvent;
