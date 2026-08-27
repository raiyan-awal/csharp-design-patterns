namespace EventSourcingPattern.Projections;

using EventSourcingPattern.Domain.Events;

public sealed class MemberSummaryProjection
{
    private readonly Dictionary<int, MemberSummary> _summaries = new();

    public void Project(IDomainEvent evt)
    {
        switch (evt)
        {
            case MemberEnrolledEvent e:
                _summaries[e.MemberId] = new MemberSummary { MemberId = e.MemberId, Name = e.Name };
                _summaries[e.MemberId].EventCount++;
                break;
            case PointsEarnedEvent e:
                _summaries[e.MemberId].PointsBalance += e.Amount;
                _summaries[e.MemberId].TotalEarned   += e.Amount;
                _summaries[e.MemberId].EventCount++;
                break;
            case PointsRedeemedEvent e:
                _summaries[e.MemberId].PointsBalance  -= e.Amount;
                _summaries[e.MemberId].TotalRedeemed  += e.Amount;
                _summaries[e.MemberId].EventCount++;
                break;
            case TierUpgradedEvent e:
                _summaries[e.MemberId].Tier = e.NewTier;
                _summaries[e.MemberId].EventCount++;
                break;
            case AccountSuspendedEvent e:
                _summaries[e.MemberId].IsSuspended = true;
                _summaries[e.MemberId].EventCount++;
                break;
            case AccountReinstatedEvent e:
                _summaries[e.MemberId].IsSuspended = false;
                _summaries[e.MemberId].EventCount++;
                break;
        }
    }

    public MemberSummary? GetSummary(int memberId) =>
        _summaries.TryGetValue(memberId, out var s) ? s : null;

    public IReadOnlyList<MemberSummary> GetAll() =>
        _summaries.Values.ToList();
}
