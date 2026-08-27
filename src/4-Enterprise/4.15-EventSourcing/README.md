# 4.15 — Event Sourcing

## Intent

Event Sourcing stores every state change as an immutable event rather than overwriting the current record. The current state is derived by replaying the event history from the beginning. The event log becomes the single source of truth — you can always rebuild any past state, produce audit trails, and build new read models by replaying events.

## The Problem It Solves

```csharp
// Without Event Sourcing: only current state is stored
UPDATE members SET points_balance = 4300, tier = 'Silver' WHERE id = 1;

// Questions you can never answer:
// - When did this member reach Silver tier?
// - What purchases drove the balance from 0 to 4300?
// - Was the balance ever higher, and if so, when was it redeemed?
// - What did the account look like last Tuesday?
```

Problems:

- **No history.** The database holds only the latest snapshot. All the events that produced it are gone.
- **No audit trail.** You cannot prove what happened, when, or why. For a rewards programme, this creates disputes you cannot resolve.
- **Irreversible bugs.** If a points calculation was wrong for three months, the correct balance cannot be reconstructed — the original transactions are lost.
- **Read model is locked in.** You can only query the shape the schema was designed for. A new business question (e.g., "show me the top earners in the last 30 days") may require a schema migration and historical data that no longer exists.

## Solution: Append-Only Event Log

```csharp
// Every change is a new event appended to the stream — nothing is overwritten
eventStore.Append(memberId, account.UncommittedEvents);

// Current state is reconstituted by replaying events
var history = eventStore.Load(memberId);
var account = MemberAccount.Reconstitute(history);

// Or from a snapshot (skip replaying thousands of old events)
var snapshot   = snapshotStore.Load(memberId)!;
var deltaEvents = eventStore.LoadFrom(memberId, snapshot.Version);
var account    = MemberAccount.ReconstituteFromSnapshot(snapshot, deltaEvents);
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Domain Event | `IDomainEvent` | Marker interface: every event has an `OccurredAt` timestamp |
| Events | `MemberEnrolledEvent`, `PointsEarnedEvent`, `PointsRedeemedEvent`, `TierUpgradedEvent`, `AccountSuspendedEvent`, `AccountReinstatedEvent` | Immutable facts about what happened |
| Aggregate | `MemberAccount` | Validates commands; raises events; rebuilds state via `When(...)` dispatch |
| Enum | `MemberTier` | `Standard` / `Silver` / `Gold` / `Platinum` — tier thresholds at 1k / 5k / 10k points |
| Event Store | `IEventStore` / `InMemoryEventStore` | Append-only log; supports `Load` (full) and `LoadFrom` (delta from a version) |
| Snapshot | `MemberSnapshot` / `ISnapshotStore` / `InMemorySnapshotStore` | Point-in-time state capture to avoid full replay |
| Projection | `MemberSummaryProjection` / `MemberSummary` | Read model built by projecting events; independent of the aggregate |

## Structure

```
4.15-EventSourcing/
├── EventSourcingPattern/
│   ├── Domain/
│   │   ├── Events/
│   │   │   ├── IDomainEvent.cs
│   │   │   ├── MemberEnrolledEvent.cs
│   │   │   ├── PointsEarnedEvent.cs
│   │   │   ├── PointsRedeemedEvent.cs
│   │   │   ├── TierUpgradedEvent.cs
│   │   │   ├── AccountSuspendedEvent.cs
│   │   │   └── AccountReinstatedEvent.cs
│   │   ├── MemberTier.cs
│   │   └── MemberAccount.cs       ← aggregate: commands, Raise, When, Reconstitute
│   ├── Infrastructure/
│   │   ├── IEventStore.cs
│   │   ├── InMemoryEventStore.cs
│   │   ├── MemberSnapshot.cs
│   │   ├── ISnapshotStore.cs
│   │   └── InMemorySnapshotStore.cs
│   ├── Projections/
│   │   ├── MemberSummary.cs
│   │   └── MemberSummaryProjection.cs
│   └── Program.cs                 ← 4-section demo
└── EventSourcingPattern.Tests/
    └── EventSourcingTests.cs      ← 33 tests across 7 suites
```

## Key Code

### MemberAccount — Raise vs ApplyHistorical

```csharp
// Called when a new command is processed
private void Raise(IDomainEvent evt)
{
    _uncommittedEvents.Add(evt);   // queued for the event store
    When(evt);                     // updates state immediately
    Version++;
}

// Called during reconstitution — does NOT add to uncommitted events
private void ApplyHistorical(IDomainEvent evt)
{
    When(evt);
    Version++;
}
```

`Raise` is used by command methods (`EarnPoints`, `Suspend`, etc.) — it both records the event for persistence and updates in-memory state. `ApplyHistorical` is used by `Reconstitute` and `ReconstituteFromSnapshot` — it replays stored events to rebuild state without re-queuing them.

### MemberAccount — When dispatch

```csharp
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

private void When(MemberEnrolledEvent e)  { Id = e.MemberId; Name = e.Name; /* ... */ }
private void When(PointsEarnedEvent e)    => PointsBalance += e.Amount;
private void When(PointsRedeemedEvent e)  => PointsBalance -= e.Amount;
private void When(TierUpgradedEvent e)    => Tier = e.NewTier;
private void When(AccountSuspendedEvent _)  => IsSuspended = true;
private void When(AccountReinstatedEvent _) => IsSuspended = false;
```

Each `When` overload is a pure state transition — no validation, no side effects. All validation lives in the command methods. This split is what makes replay safe: replaying events never re-runs business rules, it only reconstructs state.

### Automatic tier upgrade — raised as a separate event

```csharp
public void EarnPoints(int amount, string reason)
{
    if (IsSuspended) throw ...;
    Raise(new PointsEarnedEvent(Id, amount, reason, DateTime.UtcNow));

    // Tier is evaluated AFTER PointsEarned has updated PointsBalance
    var newTier = CalculateTier(PointsBalance);
    if (newTier != Tier)
        Raise(new TierUpgradedEvent(Id, Tier, newTier, DateTime.UtcNow));
}
```

The tier upgrade is a separate domain event, not a silent side effect. This means the event log records *why* the tier changed (it crossed the threshold at this exact earn), and a projection can independently track tier upgrade history.

### Snapshot — skip replaying thousands of old events

```csharp
// Save a snapshot at current state
var snapshot = account.TakeSnapshot();
snapshotStore.Save(snapshot);

// Later: load only events that happened after the snapshot
var snap        = snapshotStore.Load(memberId)!;
var deltaEvents = eventStore.LoadFrom(memberId, snap.Version);
var account     = MemberAccount.ReconstituteFromSnapshot(snap, deltaEvents);
```

`LoadFrom(streamId, fromVersion)` skips the first `fromVersion` events. If a member has 10,000 events and a snapshot at version 9,800, only 200 events need to be replayed — the other 9,800 are pre-applied via the snapshot.

### Projection — independent read model from the same events

```csharp
// After saving to the event store, project the same events to the read side
eventStore.Append(account.Id, account.UncommittedEvents);
foreach (var evt in account.UncommittedEvents)
    projection.Project(evt);
account.ClearUncommittedEvents();

// Query the read model directly — no aggregate load required
var summary = projection.GetSummary(memberId);
```

The projection and the aggregate both consume the same events but serve different purposes. The aggregate enforces business rules on the write side; the projection maintains a denormalized summary on the read side. Adding a new projection (e.g., a leaderboard) requires only a new class that handles the existing events — no schema migration, no data loss.

## Demo Scenarios

```
=== Maple Rewards Club — Event Sourcing Demo ===

--- Enrolling Members ---
  Member #1: Kenji Nakamura | Tier: Standard | Balance:    0 pts | v1
  Member #2: Priya Sharma   | Tier: Standard | Balance:    0 pts | v1

--- Earning Points & Tier Upgrades ---
  Kenji  | Tier: Gold     | Balance:   5100 pts | v6
  Priya  | Tier: Silver   | Balance:   1100 pts | v4

  Kenji's event stream:
    [hh:mm:ss] MemberEnrolledEvent
    [hh:mm:ss] PointsEarnedEvent
    [hh:mm:ss] PointsEarnedEvent
    [hh:mm:ss] TierUpgradedEvent      ← Standard → Silver at 1,100 pts
    [hh:mm:ss] PointsEarnedEvent
    [hh:mm:ss] TierUpgradedEvent      ← Silver → Gold at 5,100 pts

--- Redeeming Points & Suspension ---
  Kenji redeemed 1,000 pts → Balance: 4100 pts
  Priya suspended | IsSuspended: True
  [BLOCKED] Cannot earn points on a suspended account.
  Priya reinstated | IsSuspended: False

--- Replaying from Event History ---
  Original  → Balance: 4100 | Tier: Gold | v7
  Replayed  → Balance: 4100 | Tier: Gold | v7
  States match: True
  Snapshot saved — v7 | Balance: 4100 | Tier: Gold
  Kenji (live) → Balance: 7100 | Tier: Gold | v9
  From snapshot → Balance: 7100 | Tier: Gold | v9
  Delta events replayed: 2 (vs 9 total events for full replay)
```

## When to Use

- A complete audit trail is a business requirement — financial transactions, healthcare records, compliance logging.
- You need the ability to reconstruct past states ("what did this account look like on January 15th?").
- Your domain produces multiple types of events that different downstream systems care about independently (email, analytics, fraud detection).
- You are combining with CQRS and need the write side to produce events that feed multiple read models.

## When NOT to Use

- Simple CRUD where no history is needed and the current state is all that matters.
- High-volume entities with millions of events and no snapshot strategy — replay time becomes prohibitive.
- Teams unfamiliar with the pattern — event sourcing adds significant complexity around schema evolution, event versioning, and reconstitution that CRUD does not have.
- Entities whose state changes are trivial and have no business meaning (e.g., a UI preference toggle).

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Complete audit trail | Every state change is recorded as an immutable fact; nothing is overwritten. |
| Temporal queries | Any past state can be rebuilt by replaying events up to a given point in time. |
| Multiple projections | New read models can be built by replaying the full event history — no data migration needed. |
| Debugging | Replaying events up to the point of a bug reproduces the exact state that caused it. |
| Decoupled consumers | Downstream systems subscribe to events independently; the aggregate does not know about them. |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Event schema evolution | Adding or renaming fields in past events requires a versioning or upcasting strategy; old events never change. |
| Eventual consistency | Projections are updated asynchronously; read models may lag behind the write side briefly. |
| Replay cost | Streams with millions of events require snapshots to remain performant; without them, load times grow unboundedly. |
| Complexity | The Raise/When/Reconstitute pattern, snapshot management, and projection infrastructure are significantly more complex than a simple UPDATE statement. |

## Related Patterns

- **Domain Event (4.12)** — event sourcing and domain events share the same event type; the difference is that domain events are dispatched externally after persistence, while event sourcing events are the persistence mechanism itself.
- **CQRS (4.03)** — event sourcing is the natural write-side implementation for CQRS; events feed the command side and projections feed the query side.
- **Aggregate Root (4.13)** — the aggregate root is the consistency boundary that decides which events to raise; event sourcing is how those events are persisted.
- **Read Model / Projection (4.28)** — projections consume the event stream to build denormalized, query-optimized views; every projection is independent and can be rebuilt by replaying the full history.
- **Snapshot** — not a separate pattern in this repo, but an optimisation technique: saves aggregate state at a point in time so that only events after the snapshot need to be replayed on load.

## Running the Demo

```bash
cd src/4-Enterprise/4.15-EventSourcing/EventSourcingPattern && dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.15-EventSourcing/EventSourcingPattern.Tests && dotnet test
```
