namespace EventSourcingPattern.Infrastructure;

using EventSourcingPattern.Domain;

public sealed record MemberSnapshot(
    int        MemberId,
    string     Name,
    string     Email,
    int        PointsBalance,
    MemberTier Tier,
    bool       IsSuspended,
    int        Version);
