namespace EventSourcingPattern.Domain.Events;

public sealed record MemberEnrolledEvent(
    int      MemberId,
    string   Name,
    string   Email,
    DateTime OccurredAt) : IDomainEvent;
