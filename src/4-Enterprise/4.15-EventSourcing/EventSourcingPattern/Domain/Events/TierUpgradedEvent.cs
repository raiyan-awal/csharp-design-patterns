namespace EventSourcingPattern.Domain.Events;

using EventSourcingPattern.Domain;

public sealed record TierUpgradedEvent(
    int        MemberId,
    MemberTier PreviousTier,
    MemberTier NewTier,
    DateTime   OccurredAt) : IDomainEvent;
