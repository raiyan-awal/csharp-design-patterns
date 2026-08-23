# 4.12 — Domain Event

## Intent

A Domain Event is an immutable record of something significant that happened in the domain — something a domain expert would care about. Instead of baking side effects (sending emails, updating projections, logging to an audit trail) directly into domain methods, the domain object raises events and independent handlers react to them. This keeps the domain pure and makes side effects easy to add, remove, or change without touching domain logic.

## The Problem It Solves

```csharp
// Without Domain Events: side effects hard-coded inside domain methods
public void PlaceBid(string bidder, decimal amount)
{
    if (amount <= _currentBid) throw new InvalidOperationException("Bid too low.");
    _currentBid = amount;
    _currentWinner = bidder;

    // now the domain method does all of these too:
    _emailService.SendBidConfirmation(bidder, amount);
    _auditLog.Record($"Bid placed by {bidder}");
    _fraudDetector.Check(bidder, amount, _auctionId);
    _analyticsTracker.TrackBid(_auctionId, amount);
}
```

Problems:

- **Domain knows about infrastructure.** `_emailService`, `_auditLog`, `_fraudDetector`, and `_analyticsTracker` are all infrastructure concerns injected into a domain object that should only model bidding rules.
- **Hard to test.** Unit testing `PlaceBid` requires mocking four collaborators — and any new side effect means adding another mock everywhere.
- **Fragile coupling.** Adding fraud detection means modifying `PlaceBid`. If fraud detection throws, the bid is never recorded. The ordering of side effects is baked into the method.
- **No consistency boundary.** If the email service fails, should the bid be rolled back? Right now it's undefined.

## Solution: Raise Events; Let Handlers React

```csharp
// Domain method raises an event — no infrastructure references needed
public void PlaceBid(string bidder, decimal amount)
{
    if (amount <= CurrentBid) throw new InvalidOperationException("Bid too low.");
    CurrentBid    = amount;
    CurrentWinner = bidder;
    Raise(new BidPlacedEvent(Id, bidder, amount, _bids.Count, DateTime.UtcNow));
}

// Application layer: save the auction, then dispatch events
auction.PlaceBid("Priya Nair", 920_000m);
_repository.Save(auction);                      // persistence succeeds first
_dispatcher.DispatchAndClear(auction);          // side effects run after commit
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Domain Event | `IDomainEvent` | Marker interface with `OccurredAt` timestamp |
| Domain Events | `AuctionOpenedEvent`, `BidPlacedEvent`, `AuctionClosedEvent` | Immutable records of what happened |
| Event Handler | `IDomainEventHandler<TEvent>` | Contract for a handler of a specific event type |
| Handlers | `AuditLogHandler`, `EmailNotificationHandler`, `FraudDetectionHandler` | Independent side effects; each handles only what it cares about |
| Aggregate Root | `AggregateRoot` | Base class that collects raised events in a list; cleared after dispatch |
| Domain Entity | `Auction` | Raises events during `PlaceBid` and `Close`; inherits from `AggregateRoot` |
| Dispatcher | `DomainEventDispatcher` | Routes events to registered handlers; `DispatchAndClear` is the standard call |

## Structure

```
4.12-DomainEvent/
├── DomainEventPattern/
│   ├── Events/
│   │   ├── IDomainEvent.cs           ← marker interface with OccurredAt
│   │   ├── AuctionOpenedEvent.cs     ← record: AuctionId, Title, ReservePrice, OccurredAt
│   │   ├── BidPlacedEvent.cs         ← record: AuctionId, Bidder, Amount, BidNumber, OccurredAt
│   │   └── AuctionClosedEvent.cs     ← record: AuctionId, Title, Winner?, WinningBid, ReserveMet, OccurredAt
│   ├── Handlers/
│   │   ├── IDomainEventHandler.cs    ← generic handler interface
│   │   ├── AuditLogHandler.cs        ← handles all three event types; builds log list
│   │   ├── EmailNotificationHandler.cs ← handles BidPlaced + AuctionClosed; collects sent emails
│   │   └── FraudDetectionHandler.cs  ← handles BidPlaced; detects shill bidding
│   ├── Domain/
│   │   ├── AggregateRoot.cs          ← DomainEvents list, Raise(), ClearEvents()
│   │   └── Auction.cs                ← raises events; enforces bidding invariants
│   ├── Infrastructure/
│   │   └── DomainEventDispatcher.cs  ← Register<TEvent>, Dispatch, DispatchAndClear
│   └── Program.cs                    ← 5-section demo: open, bid, fraud, close, reserve-not-met
└── DomainEventPattern.Tests/
    └── DomainEventTests.cs           ← 20 tests across aggregate, dispatcher, and handler suites
```

## Key Code

### Aggregate Root — collecting events without dispatching them

```csharp
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearEvents() => _domainEvents.Clear();
}
```

Events are collected in a private list. The domain object never dispatches them itself — it just records what happened. The application layer reads `DomainEvents` after saving the aggregate and dispatches them. This guarantees handlers never run on a transaction that failed to commit.

### Auction — pure domain logic, no infrastructure

```csharp
public void PlaceBid(string bidder, decimal amount)
{
    if (Status != AuctionStatus.Open)
        throw new InvalidOperationException("Bids cannot be placed on a closed auction.");
    if (amount <= CurrentBid)
        throw new InvalidOperationException($"Bid of ${amount:N2} must exceed the current bid.");

    _bids.Add((bidder, amount));
    CurrentBid    = amount;
    CurrentWinner = bidder;
    Raise(new BidPlacedEvent(Id, bidder, amount, _bids.Count, DateTime.UtcNow));
}
```

`PlaceBid` has zero knowledge of email, audit logs, or fraud detection. It enforces bidding rules and raises an event. Any number of handlers can be added or removed without touching this method.

### Dispatcher — type-safe routing without reflection at dispatch time

```csharp
public void Register<TEvent>(IDomainEventHandler<TEvent> handler) where TEvent : IDomainEvent
{
    var key = typeof(TEvent);
    if (!_handlers.ContainsKey(key)) _handlers[key] = new();
    _handlers[key].Add(e => handler.Handle((TEvent)e));
}

public void Dispatch(IEnumerable<IDomainEvent> events)
{
    foreach (var domainEvent in events)
        if (_handlers.TryGetValue(domainEvent.GetType(), out var actions))
            foreach (var action in actions)
                action(domainEvent);
}
```

At registration time, the strongly-typed `Handle(TEvent)` call is wrapped in an `Action<IDomainEvent>` delegate. At dispatch time, the cast `(TEvent)e` is safe because the dictionary key guarantees the runtime type matches. No reflection required at dispatch — just a dictionary lookup and delegate invocation.

### Fraud Detection Handler — independent, focused concern

```csharp
public sealed class FraudDetectionHandler : IDomainEventHandler<BidPlacedEvent>
{
    private readonly Dictionary<int, string?> _currentWinner = new();

    public void Handle(BidPlacedEvent e)
    {
        if (_currentWinner.TryGetValue(e.AuctionId, out var prev) && prev == e.Bidder)
            _alerts.Add($"[FRAUD] Shill bid: '{e.Bidder}' raised own winning bid to ${e.Amount:N2} CAD");
        _currentWinner[e.AuctionId] = e.Bidder;
    }
}
```

This handler only knows about `BidPlacedEvent`. It has its own internal state (`_currentWinner`). It can be added to or removed from the dispatcher with a single `Register` / no-register call and zero changes to `Auction`.

## Demo Scenarios

```
=== Maple Auctions — Domain Event Demo ===

--- Opening Auctions ---
[AUDIT] Auction #1 'Group of Seven Landscape — Lawren Harris (1924)' opened (reserve $850,000.00 CAD)
[AUDIT] Auction #2 'Inuit Soapstone Sculpture — Kenojuak Ashevak' opened (reserve $12,000.00 CAD)

--- Bidding on Group of Seven Landscape ---
[AUDIT] Bid #1 on auction #1: Laurent Beauchamp — $860,000.00 CAD  [EMAIL] New bid...
[AUDIT] Bid #2 on auction #1: Priya Nair — $890,000.00 CAD         [EMAIL] New bid...
...

--- Bidding on Inuit Sculpture (shill bid scenario) ---
[FRAUD] Shill bid detected: 'Sandra Chu' raised own winning bid to $15,000.00 CAD

--- Closing Auctions ---
[EMAIL] Auction 'Group of Seven Landscape' closed — winner: Lauren Beauchamp at $950,000.00 CAD

--- Auction Where Reserve Is Not Met ---
[EMAIL] Auction 'Contemporary Ottawa Sculpture' closed — reserve not met (highest: $42,000.00 CAD)
```

## When to Use

- A domain operation has side effects that belong to different concerns (notifications, auditing, projections, analytics) and you want those concerns decoupled from the domain model.
- You need to be sure side effects only run after the domain operation and its persistence succeed — not if a transaction rolls back.
- You want to evolve side effects independently: add an SMS handler, remove a legacy audit handler, change email format — without touching domain logic.
- You are implementing CQRS and need to update a read model every time the write model changes.

## When NOT to Use

- The side effect is part of the core domain invariant — if sending a confirmation email is a *rule* (not a side effect), it belongs in the domain method, not a handler.
- You have a simple CRUD application where the overhead of an event infrastructure exceeds the benefit.
- The side effect must happen synchronously inside the same transaction (e.g., a read-model update that the next line of code immediately reads back) — deferred dispatch won't work without additional infrastructure.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Decoupled side effects | Handlers are independent; adding or removing one touches nothing in the domain. |
| Testable domain | Domain methods can be unit-tested with zero mocks — they raise events, tests assert events. |
| Transactional safety | Events are only dispatched after persistence succeeds; handlers never run on a rolled-back transaction. |
| Open/Closed | New reactions to existing events (a new handler type) require no changes to existing code. |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Eventual consistency | If a handler fails after dispatch, the domain state is already committed; the side effect is lost unless you add retry/inbox infrastructure. |
| Harder to trace | Control flow is indirect — a bug in a handler is several hops away from the domain method that triggered it. |
| In-process only | This implementation is in-memory; cross-service events need a message broker (Kafka, RabbitMQ, Azure Service Bus). |

## Related Patterns

- **Aggregate Root (4.13)** — the natural owner of domain events; the aggregate raises events when its invariants are enforced and its state changes.
- **CQRS (4.03)** — domain events are the bridge between the write side and the read side; each event updates one or more read models via a handler.
- **Value Object (4.11)** — domain events carry value objects as payload (e.g., `BidPlacedEvent` could carry a `Money` amount rather than a raw `decimal`).
- **Observer (3.07)** — domain events are the domain-layer application of the Observer pattern; the difference is that domain events are always past-tense facts about the domain, not general notifications.

## Running the Demo

```bash
cd src/4-Enterprise/4.12-DomainEvent/DomainEventPattern && dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.12-DomainEvent/DomainEventPattern.Tests && dotnet test
```
