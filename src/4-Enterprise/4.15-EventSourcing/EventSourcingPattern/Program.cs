using EventSourcingPattern.Domain;
using EventSourcingPattern.Infrastructure;
using EventSourcingPattern.Projections;

Console.WriteLine("=== Maple Rewards Club — Event Sourcing Demo ===\n");

var eventStore    = new InMemoryEventStore();
var snapshotStore = new InMemorySnapshotStore();
var projection    = new MemberSummaryProjection();

void SaveAndProject(MemberAccount account)
{
    eventStore.Append(account.Id, account.UncommittedEvents);
    foreach (var evt in account.UncommittedEvents)
        projection.Project(evt);
    account.ClearUncommittedEvents();
}

// ── Section 1: Enrolling Members ──────────────────────────────────────────
Console.WriteLine("--- Enrolling Members ---");

var kenji = MemberAccount.Enroll(1, "Kenji Nakamura", "kenji@example.ca");
var priya = MemberAccount.Enroll(2, "Priya Sharma",   "priya@example.ca");

SaveAndProject(kenji);
SaveAndProject(priya);

Console.WriteLine($"  Member #{kenji.Id}: {kenji.Name} | Tier: {kenji.Tier} | Balance: {kenji.PointsBalance} pts | v{kenji.Version}");
Console.WriteLine($"  Member #{priya.Id}: {priya.Name} | Tier: {priya.Tier} | Balance: {priya.PointsBalance} pts | v{priya.Version}");

Pause();

// ── Section 2: Earning Points & Tier Upgrades ─────────────────────────────
Console.WriteLine("--- Earning Points & Tier Upgrades ---");

kenji.EarnPoints(500,  "Groceries at Metro");
kenji.EarnPoints(600,  "Purchases at Canadian Tire");   // crosses 1,000 → Silver
kenji.EarnPoints(4000, "Holiday bonus points");          // crosses 5,000 → Gold
SaveAndProject(kenji);

priya.EarnPoints(300, "Fuel at Petro-Canada");
priya.EarnPoints(800, "Purchases at Hudson's Bay");      // crosses 1,000 → Silver
SaveAndProject(priya);

Console.WriteLine($"  Kenji  | Tier: {kenji.Tier,-8} | Balance: {kenji.PointsBalance,6} pts | v{kenji.Version}");
Console.WriteLine($"  Priya  | Tier: {priya.Tier,-8} | Balance: {priya.PointsBalance,6} pts | v{priya.Version}");

Console.WriteLine("\n  Kenji's event stream:");
foreach (var evt in eventStore.Load(1))
    Console.WriteLine($"    [{evt.OccurredAt:HH:mm:ss}] {evt.GetType().Name}");

Pause();

// ── Section 3: Redeeming Points & Suspension ──────────────────────────────
Console.WriteLine("--- Redeeming Points & Suspension ---");

kenji.RedeemPoints(1000, "Redeemed for $10 Maple Rewards gift card");
SaveAndProject(kenji);
Console.WriteLine($"  Kenji redeemed 1,000 pts → Balance: {kenji.PointsBalance} pts");

priya.Suspend("Suspected fraudulent activity — account under review");
SaveAndProject(priya);
Console.WriteLine($"  Priya suspended | IsSuspended: {priya.IsSuspended}");

try { priya.EarnPoints(100, "Should fail"); }
catch (InvalidOperationException ex) { Console.WriteLine($"  [BLOCKED] {ex.Message}"); }

priya.Reinstate();
SaveAndProject(priya);
Console.WriteLine($"  Priya reinstated | IsSuspended: {priya.IsSuspended}");

Pause();

// ── Section 4: Replaying from Event History ───────────────────────────────
Console.WriteLine("--- Replaying from Event History ---");

Console.WriteLine("  Loading Kenji's full event stream and reconstituting from scratch...");
var kenjiHistory    = eventStore.Load(1);
var kenjiReplayed   = MemberAccount.Reconstitute(kenjiHistory);

Console.WriteLine($"  Original  → Balance: {kenji.PointsBalance} | Tier: {kenji.Tier} | v{kenji.Version}");
Console.WriteLine($"  Replayed  → Balance: {kenjiReplayed.PointsBalance} | Tier: {kenjiReplayed.Tier} | v{kenjiReplayed.Version}");
Console.WriteLine($"  States match: {kenji.PointsBalance == kenjiReplayed.PointsBalance && kenji.Tier == kenjiReplayed.Tier}");

Console.WriteLine("\n  Taking a snapshot of Kenji at current state...");
var snapshot = kenji.TakeSnapshot();
snapshotStore.Save(snapshot);
Console.WriteLine($"  Snapshot saved — v{snapshot.Version} | Balance: {snapshot.PointsBalance} | Tier: {snapshot.Tier}");

kenji.EarnPoints(3000, "Year-end bonus");    // crosses 10,000 → Platinum
SaveAndProject(kenji);

Console.WriteLine("\n  New activity after snapshot:");
Console.WriteLine($"  Kenji (live) → Balance: {kenji.PointsBalance} | Tier: {kenji.Tier} | v{kenji.Version}");

var loadedSnapshot  = snapshotStore.Load(1)!;
var deltaEvents     = eventStore.LoadFrom(1, loadedSnapshot.Version);
var kenjiFromSnap   = MemberAccount.ReconstituteFromSnapshot(loadedSnapshot, deltaEvents);

Console.WriteLine($"  From snapshot → Balance: {kenjiFromSnap.PointsBalance} | Tier: {kenjiFromSnap.Tier} | v{kenjiFromSnap.Version}");
Console.WriteLine($"  Delta events replayed: {deltaEvents.Count} (vs {eventStore.Load(1).Count} total events for full replay)");

Console.WriteLine("\n  Projection read model (all members):");
foreach (var summary in projection.GetAll())
    Console.WriteLine($"    #{summary.MemberId} {summary.Name,-15} | Balance: {summary.PointsBalance,6} | Tier: {summary.Tier,-8} | Earned: {summary.TotalEarned} | Redeemed: {summary.TotalRedeemed}");

Console.WriteLine("\n=== Demo complete ===");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
