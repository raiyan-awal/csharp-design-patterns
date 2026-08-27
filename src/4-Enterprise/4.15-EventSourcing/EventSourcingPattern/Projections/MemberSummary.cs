namespace EventSourcingPattern.Projections;

using EventSourcingPattern.Domain;

public sealed class MemberSummary
{
    public int        MemberId      { get; internal set; }
    public string     Name          { get; internal set; } = "";
    public int        PointsBalance { get; internal set; }
    public MemberTier Tier          { get; internal set; }
    public bool       IsSuspended   { get; internal set; }
    public int        TotalEarned   { get; internal set; }
    public int        TotalRedeemed { get; internal set; }
    public int        EventCount    { get; internal set; }
}
