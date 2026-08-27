using EventSourcingPattern.Domain;
using EventSourcingPattern.Domain.Events;
using EventSourcingPattern.Infrastructure;
using EventSourcingPattern.Projections;

namespace EventSourcingPattern.Tests;

// ── Enrollment ────────────────────────────────────────────────────────────

public class MemberEnrollmentTests
{
    [Fact]
    public void Enroll_ProducesEnrolledEvent()
    {
        var account = MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");
        Assert.Single(account.UncommittedEvents);
        Assert.IsType<MemberEnrolledEvent>(account.UncommittedEvents[0]);
    }

    [Fact]
    public void Enroll_SetsInitialState()
    {
        var account = MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");
        Assert.Equal(1, account.Id);
        Assert.Equal("Kenji Nakamura", account.Name);
        Assert.Equal("kenji@example.ca", account.Email);
        Assert.Equal(0, account.PointsBalance);
        Assert.Equal(MemberTier.Standard, account.Tier);
        Assert.False(account.IsSuspended);
    }

    [Fact]
    public void Enroll_StartsAtVersionOne()
    {
        var account = MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");
        Assert.Equal(1, account.Version);
    }
}

// ── Points earning ────────────────────────────────────────────────────────

public class PointsEarningTests
{
    private static MemberAccount Enrolled() =>
        MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");

    [Fact]
    public void EarnPoints_IncreasesBalance()
    {
        var account = Enrolled();
        account.ClearUncommittedEvents();
        account.EarnPoints(500, "Groceries at Metro");
        Assert.Equal(500, account.PointsBalance);
    }

    [Fact]
    public void EarnPoints_ProducesPointsEarnedEvent()
    {
        var account = Enrolled();
        account.ClearUncommittedEvents();
        account.EarnPoints(500, "Groceries at Metro");
        Assert.Single(account.UncommittedEvents);
        Assert.IsType<PointsEarnedEvent>(account.UncommittedEvents[0]);
    }

    [Fact]
    public void EarnPoints_CrossingTierThreshold_TriggersUpgrade()
    {
        var account = Enrolled();
        account.EarnPoints(1000, "Bonus");
        Assert.Equal(MemberTier.Silver, account.Tier);
    }

    [Fact]
    public void EarnPoints_TierUpgrade_ProducesTwoEvents()
    {
        var account = Enrolled();
        account.ClearUncommittedEvents();
        account.EarnPoints(1000, "Bonus");  // crosses Standard → Silver
        Assert.Equal(2, account.UncommittedEvents.Count);
        Assert.IsType<PointsEarnedEvent>(account.UncommittedEvents[0]);
        Assert.IsType<TierUpgradedEvent>(account.UncommittedEvents[1]);
    }

    [Fact]
    public void EarnPoints_OnSuspendedAccount_Throws()
    {
        var account = Enrolled();
        account.Suspend("Test suspension");
        Assert.Throws<InvalidOperationException>(() => account.EarnPoints(100, "Groceries"));
    }

    [Fact]
    public void EarnPoints_ZeroAmount_Throws()
    {
        var account = Enrolled();
        Assert.Throws<ArgumentException>(() => account.EarnPoints(0, "Reason"));
    }
}

// ── Points redemption ─────────────────────────────────────────────────────

public class PointsRedemptionTests
{
    private static MemberAccount WithBalance(int points)
    {
        var account = MemberAccount.Enroll(1, "Priya Sharma", "priya@example.ca");
        account.EarnPoints(points, "Setup");
        account.ClearUncommittedEvents();
        return account;
    }

    [Fact]
    public void RedeemPoints_DecreasesBalance()
    {
        var account = WithBalance(500);
        account.RedeemPoints(200, "Gift card");
        Assert.Equal(300, account.PointsBalance);
    }

    [Fact]
    public void RedeemPoints_ProducesRedeemedEvent()
    {
        var account = WithBalance(500);
        account.RedeemPoints(200, "Gift card");
        Assert.Single(account.UncommittedEvents);
        Assert.IsType<PointsRedeemedEvent>(account.UncommittedEvents[0]);
    }

    [Fact]
    public void RedeemPoints_MoreThanBalance_Throws()
    {
        var account = WithBalance(300);
        Assert.Throws<InvalidOperationException>(() => account.RedeemPoints(500, "Gift card"));
    }

    [Fact]
    public void RedeemPoints_OnSuspendedAccount_Throws()
    {
        var account = WithBalance(500);
        account.Suspend("Fraud review");
        Assert.Throws<InvalidOperationException>(() => account.RedeemPoints(100, "Gift card"));
    }
}

// ── Suspension ────────────────────────────────────────────────────────────

public class SuspensionTests
{
    private static MemberAccount Enrolled() =>
        MemberAccount.Enroll(1, "Priya Sharma", "priya@example.ca");

    [Fact]
    public void Suspend_SetsSuspendedFlag()
    {
        var account = Enrolled();
        account.Suspend("Fraud review");
        Assert.True(account.IsSuspended);
    }

    [Fact]
    public void Suspend_AlreadySuspended_Throws()
    {
        var account = Enrolled();
        account.Suspend("Fraud review");
        Assert.Throws<InvalidOperationException>(() => account.Suspend("Again"));
    }

    [Fact]
    public void Reinstate_ClearsSuspendedFlag()
    {
        var account = Enrolled();
        account.Suspend("Fraud review");
        account.Reinstate();
        Assert.False(account.IsSuspended);
    }

    [Fact]
    public void Reinstate_NotSuspended_Throws()
    {
        var account = Enrolled();
        Assert.Throws<InvalidOperationException>(() => account.Reinstate());
    }
}

// ── Event replay ──────────────────────────────────────────────────────────

public class EventReplayTests
{
    [Fact]
    public void Reconstitute_FromHistory_ProducesSameState()
    {
        var account = MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");
        account.EarnPoints(1500, "Bonus");   // triggers Silver upgrade
        account.RedeemPoints(200, "Gift card");
        var history = account.UncommittedEvents.ToList();

        var replayed = MemberAccount.Reconstitute(history);

        Assert.Equal(account.Id,            replayed.Id);
        Assert.Equal(account.PointsBalance, replayed.PointsBalance);
        Assert.Equal(account.Tier,          replayed.Tier);
        Assert.Equal(account.Version,       replayed.Version);
    }

    [Fact]
    public void Reconstitute_WithTierUpgradeEvents_RestoresTier()
    {
        var account = MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");
        account.EarnPoints(5000, "Big purchase");  // Gold tier
        var history = account.UncommittedEvents.ToList();

        var replayed = MemberAccount.Reconstitute(history);

        Assert.Equal(MemberTier.Gold, replayed.Tier);
    }

    [Fact]
    public void Reconstitute_PreservesVersionCount()
    {
        var account = MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");
        account.EarnPoints(100, "Purchase");
        account.EarnPoints(200, "Purchase");
        var history = account.UncommittedEvents.ToList();

        var replayed = MemberAccount.Reconstitute(history);

        Assert.Equal(account.Version, replayed.Version);
    }

    [Fact]
    public void Reconstituted_Account_HasNoUncommittedEvents()
    {
        var account = MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");
        account.EarnPoints(500, "Purchase");
        var history = account.UncommittedEvents.ToList();

        var replayed = MemberAccount.Reconstitute(history);

        Assert.Empty(replayed.UncommittedEvents);
    }
}

// ── Snapshots ─────────────────────────────────────────────────────────────

public class SnapshotTests
{
    [Fact]
    public void TakeSnapshot_CapturesCurrentState()
    {
        var account = MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");
        account.EarnPoints(1500, "Bonus");

        var snapshot = account.TakeSnapshot();

        Assert.Equal(account.Id,            snapshot.MemberId);
        Assert.Equal(account.Name,          snapshot.Name);
        Assert.Equal(account.PointsBalance, snapshot.PointsBalance);
        Assert.Equal(account.Tier,          snapshot.Tier);
        Assert.Equal(account.Version,       snapshot.Version);
    }

    [Fact]
    public void ReconstituteFromSnapshot_RestoresBaseState()
    {
        var account  = MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");
        account.EarnPoints(1500, "Bonus");
        var snapshot = account.TakeSnapshot();

        var restored = MemberAccount.ReconstituteFromSnapshot(snapshot, []);

        Assert.Equal(account.PointsBalance, restored.PointsBalance);
        Assert.Equal(account.Tier,          restored.Tier);
        Assert.Equal(account.Version,       restored.Version);
    }

    [Fact]
    public void ReconstituteFromSnapshot_WithDeltaEvents_AppliesDeltas()
    {
        var account  = MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");
        account.EarnPoints(500, "Purchase");
        account.ClearUncommittedEvents();
        var snapshot = account.TakeSnapshot();

        // New events after snapshot
        account.EarnPoints(600, "More purchases");  // crosses Silver threshold
        var deltaEvents = account.UncommittedEvents.ToList();

        var restored = MemberAccount.ReconstituteFromSnapshot(snapshot, deltaEvents);

        Assert.Equal(account.PointsBalance, restored.PointsBalance);
        Assert.Equal(MemberTier.Silver,     restored.Tier);
        Assert.Equal(account.Version,       restored.Version);
    }

    [Fact]
    public void ReconstituteFromSnapshot_IsEquivalentToFullReplay()
    {
        var eventStore    = new InMemoryEventStore();
        var snapshotStore = new InMemorySnapshotStore();

        var account = MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");
        account.EarnPoints(500, "Purchase 1");
        account.EarnPoints(700, "Purchase 2");
        eventStore.Append(1, account.UncommittedEvents);
        account.ClearUncommittedEvents();

        snapshotStore.Save(account.TakeSnapshot());

        account.EarnPoints(4000, "Holiday bonus");
        eventStore.Append(1, account.UncommittedEvents);
        account.ClearUncommittedEvents();

        var snap         = snapshotStore.Load(1)!;
        var deltaEvents  = eventStore.LoadFrom(1, snap.Version);
        var fromSnapshot = MemberAccount.ReconstituteFromSnapshot(snap, deltaEvents);
        var fromFull     = MemberAccount.Reconstitute(eventStore.Load(1));

        Assert.Equal(fromFull.PointsBalance, fromSnapshot.PointsBalance);
        Assert.Equal(fromFull.Tier,          fromSnapshot.Tier);
        Assert.Equal(fromFull.Version,       fromSnapshot.Version);
    }
}

// ── Event store ───────────────────────────────────────────────────────────

public class EventStoreTests
{
    [Fact]
    public void Append_ThenLoad_ReturnsAllEvents()
    {
        var store   = new InMemoryEventStore();
        var account = MemberAccount.Enroll(1, "Test", "t@t.ca");
        account.EarnPoints(100, "Purchase");
        store.Append(1, account.UncommittedEvents);

        var loaded = store.Load(1);

        Assert.Equal(2, loaded.Count);  // Enrolled + PointsEarned
    }

    [Fact]
    public void LoadFrom_ReturnsEventsFromVersion()
    {
        var store   = new InMemoryEventStore();
        var account = MemberAccount.Enroll(1, "Test", "t@t.ca");
        store.Append(1, account.UncommittedEvents);
        account.ClearUncommittedEvents();

        // Version is 1 after enroll — so LoadFrom(1, 1) skips the first event
        account.EarnPoints(100, "Purchase");
        store.Append(1, account.UncommittedEvents);

        var delta = store.LoadFrom(1, 1);
        Assert.Single(delta);
        Assert.IsType<PointsEarnedEvent>(delta[0]);
    }

    [Fact]
    public void Load_NonExistent_ReturnsEmpty()
    {
        var store = new InMemoryEventStore();
        Assert.Empty(store.Load(99));
    }

    [Fact]
    public void Append_MultipleStreams_AreIsolated()
    {
        var store = new InMemoryEventStore();
        store.Append(1, MemberAccount.Enroll(1, "A", "a@a.ca").UncommittedEvents);
        store.Append(2, MemberAccount.Enroll(2, "B", "b@b.ca").UncommittedEvents);

        Assert.Single(store.Load(1));
        Assert.Single(store.Load(2));
    }
}

// ── Projection ────────────────────────────────────────────────────────────

public class ProjectionTests
{
    [Fact]
    public void Project_EnrollEvent_CreatesSummary()
    {
        var projection = new MemberSummaryProjection();
        var account    = MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");
        foreach (var evt in account.UncommittedEvents) projection.Project(evt);

        var summary = projection.GetSummary(1);
        Assert.NotNull(summary);
        Assert.Equal("Kenji Nakamura", summary.Name);
        Assert.Equal(0, summary.PointsBalance);
    }

    [Fact]
    public void Project_PointsEarned_UpdatesBalanceAndTotalEarned()
    {
        var projection = new MemberSummaryProjection();
        var account    = MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");
        account.EarnPoints(500, "Purchase");
        foreach (var evt in account.UncommittedEvents) projection.Project(evt);

        var summary = projection.GetSummary(1)!;
        Assert.Equal(500, summary.PointsBalance);
        Assert.Equal(500, summary.TotalEarned);
    }

    [Fact]
    public void Project_TierUpgraded_UpdatesTier()
    {
        var projection = new MemberSummaryProjection();
        var account    = MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");
        account.EarnPoints(1000, "Big purchase");  // triggers Silver upgrade
        foreach (var evt in account.UncommittedEvents) projection.Project(evt);

        Assert.Equal(MemberTier.Silver, projection.GetSummary(1)!.Tier);
    }

    [Fact]
    public void Project_Suspended_SetsSuspendedFlag()
    {
        var projection = new MemberSummaryProjection();
        var account    = MemberAccount.Enroll(1, "Priya Sharma", "priya@example.ca");
        account.Suspend("Fraud");
        foreach (var evt in account.UncommittedEvents) projection.Project(evt);

        Assert.True(projection.GetSummary(1)!.IsSuspended);
    }
}
