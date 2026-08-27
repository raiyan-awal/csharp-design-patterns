namespace EventSourcingPattern.Domain;

using EventSourcingPattern.Domain.Events;
using EventSourcingPattern.Infrastructure;

public sealed class MemberAccount
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();

    public int        Id            { get; private set; }
    public string     Name          { get; private set; } = "";
    public string     Email         { get; private set; } = "";
    public int        PointsBalance { get; private set; }
    public MemberTier Tier          { get; private set; }
    public bool       IsSuspended   { get; private set; }
    public int        Version       { get; private set; }

    public IReadOnlyList<IDomainEvent> UncommittedEvents => _uncommittedEvents;

    private MemberAccount() { }

    private MemberAccount(MemberSnapshot snapshot)
    {
        Id            = snapshot.MemberId;
        Name          = snapshot.Name;
        Email         = snapshot.Email;
        PointsBalance = snapshot.PointsBalance;
        Tier          = snapshot.Tier;
        IsSuspended   = snapshot.IsSuspended;
        Version       = snapshot.Version;
    }

    // ── Commands ──────────────────────────────────────────────────────────

    public static MemberAccount Enroll(int id, string name, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        var account = new MemberAccount();
        account.Raise(new MemberEnrolledEvent(id, name, email, DateTime.UtcNow));
        return account;
    }

    public void EarnPoints(int amount, string reason)
    {
        if (IsSuspended)
            throw new InvalidOperationException("Cannot earn points on a suspended account.");
        if (amount <= 0)
            throw new ArgumentException("Points amount must be positive.", nameof(amount));
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Raise(new PointsEarnedEvent(Id, amount, reason, DateTime.UtcNow));

        // Tier is checked after the balance update has been applied
        var newTier = CalculateTier(PointsBalance);
        if (newTier != Tier)
            Raise(new TierUpgradedEvent(Id, Tier, newTier, DateTime.UtcNow));
    }

    public void RedeemPoints(int amount, string reason)
    {
        if (IsSuspended)
            throw new InvalidOperationException("Cannot redeem points on a suspended account.");
        if (amount <= 0)
            throw new ArgumentException("Points amount must be positive.", nameof(amount));
        if (amount > PointsBalance)
            throw new InvalidOperationException(
                $"Cannot redeem {amount} points — current balance is {PointsBalance}.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Raise(new PointsRedeemedEvent(Id, amount, reason, DateTime.UtcNow));
    }

    public void Suspend(string reason)
    {
        if (IsSuspended)
            throw new InvalidOperationException("Account is already suspended.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Raise(new AccountSuspendedEvent(Id, reason, DateTime.UtcNow));
    }

    public void Reinstate()
    {
        if (!IsSuspended)
            throw new InvalidOperationException("Account is not suspended.");
        Raise(new AccountReinstatedEvent(Id, DateTime.UtcNow));
    }

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    public MemberSnapshot TakeSnapshot() =>
        new(Id, Name, Email, PointsBalance, Tier, IsSuspended, Version);

    // ── Reconstitution ────────────────────────────────────────────────────

    public static MemberAccount Reconstitute(IEnumerable<IDomainEvent> history)
    {
        var account = new MemberAccount();
        foreach (var evt in history)
            account.ApplyHistorical(evt);
        return account;
    }

    public static MemberAccount ReconstituteFromSnapshot(
        MemberSnapshot snapshot, IEnumerable<IDomainEvent> eventsAfterSnapshot)
    {
        var account = new MemberAccount(snapshot);
        foreach (var evt in eventsAfterSnapshot)
            account.ApplyHistorical(evt);
        return account;
    }

    // ── Event application ─────────────────────────────────────────────────

    private void Raise(IDomainEvent evt)
    {
        _uncommittedEvents.Add(evt);
        When(evt);
        Version++;
    }

    private void ApplyHistorical(IDomainEvent evt)
    {
        When(evt);
        Version++;
    }

    private void When(IDomainEvent evt)
    {
        switch (evt)
        {
            case MemberEnrolledEvent e:    When(e); break;
            case PointsEarnedEvent e:      When(e); break;
            case PointsRedeemedEvent e:    When(e); break;
            case TierUpgradedEvent e:      When(e); break;
            case AccountSuspendedEvent e:  When(e); break;
            case AccountReinstatedEvent e: When(e); break;
        }
    }

    private void When(MemberEnrolledEvent e)
    {
        Id    = e.MemberId;
        Name  = e.Name;
        Email = e.Email;
        Tier  = MemberTier.Standard;
    }

    private void When(PointsEarnedEvent e)    => PointsBalance += e.Amount;
    private void When(PointsRedeemedEvent e)  => PointsBalance -= e.Amount;
    private void When(TierUpgradedEvent e)    => Tier = e.NewTier;
    private void When(AccountSuspendedEvent _)  => IsSuspended = true;
    private void When(AccountReinstatedEvent _) => IsSuspended = false;

    private static MemberTier CalculateTier(int points) => points switch
    {
        >= 10_000 => MemberTier.Platinum,
        >= 5_000  => MemberTier.Gold,
        >= 1_000  => MemberTier.Silver,
        _         => MemberTier.Standard
    };
}
