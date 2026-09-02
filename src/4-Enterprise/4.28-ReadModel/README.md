# 4.28 — Read Model / Projection

## Intent

The Read Model / Projection pattern maintains one or more denormalized, query-optimized views that are built and kept current by listening to events from a write side. Each projection is a stateless function that knows how to update one specific read model in response to events. Because the event history is append-only and never discarded, a projection can always be rebuilt from scratch — making read models disposable, replaceable, and independently evolvable from the write model.

## The Problem It Solves

Without projections, queries share the same normalized write model, which is optimized for consistency rather than reads:

```csharp
// Without projections: every query joins across multiple tables or collections
public SellerDashboard GetSellerDashboard(string sellerId)
{
    var products = _db.Products.Where(p => p.SellerId == sellerId).ToList();
    var sales    = _db.Sales.Where(s => s.SellerId == sellerId).ToList();
    var reviews  = _db.Reviews.Where(r => products.Any(p => p.Id == r.ProductId)).ToList();
    // join, aggregate, compute averages — all at query time, every time
    return new SellerDashboard { ... };
}
```

Problems this creates:
- **Query complexity** — every dashboard or report aggregates at query time, repeating the same joins across all callers.
- **Read/write coupling** — changing the write model for a new business rule forces changes to every read query that depends on the same tables.
- **No tailoring** — the same normalized schema must serve every read shape: browsing grids, dashboards, reports, and search.
- **Projection lag** — there is no clean way to rebuild a query result from first principles if a bug corrupted historical data.

## Solution: Dedicated Projections and Read Models

A `ProjectionEngine` stores events and fans them out to registered projections. Each projection listens for the events it cares about and updates its own read model store. `Rebuild()` replays every stored event through all projections — so a new projection added months later can catch up instantly, and a buggy projection can be corrected and replayed without re-doing any business transactions.

```csharp
// Same events — two completely different read models built independently
engine.Register(new ProductCatalogueProjection(catalogueStore));  // per-product view
engine.Register(new SellerSummaryProjection(sellerStore));        // per-seller dashboard

engine.Append(new ProductListedEvent("prod-1", "seller-a", "MEC Boots", 149.95m, 30, now));
engine.Append(new ProductSoldEvent ("prod-1", "seller-a", 5, 149.95m, now));

// Two queries, two shapes, no joins
var product = catalogueStore.Get("prod-1");       // stock, rating, price
var seller  = sellerStore.Get("seller-a");        // revenue, units sold, listing count
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Domain event | `ProductListedEvent`, `ProductSoldEvent`, `ProductPriceUpdatedEvent`, `ReviewPostedEvent` | Immutable records; the source of truth |
| Event store | `IEventStore` / `InMemoryEventStore` | Append-only log of all domain events |
| Projection interface | `IProjection` | `Apply(event)` updates the read model; `Reset()` clears it for rebuild |
| Projection | `ProductCatalogueProjection` | Builds per-product view (stock, price, rating) |
| Projection | `SellerSummaryProjection` | Builds per-seller view (listings, units sold, revenue) |
| Read model store | `IReadModelStore<TKey, TView>` / `InMemoryReadModelStore<TKey, TView>` | Key-value store for one read model type |
| Read model | `ProductCatalogueView` | Denormalized product data optimized for browsing |
| Read model | `SellerSummaryView` | Denormalized seller data optimized for the dashboard |
| Projection engine | `ProjectionEngine` | Routes `Append` to all projections; implements `Rebuild` |
| Application service | `MarketplaceService` | Converts user actions into events; reads from stores |

## Structure

```
4.28-ReadModel/
├── ReadModelPattern/
│   ├── Events/
│   │   ├── IDomainEvent.cs              ← interface with OccurredAt
│   │   ├── ProductListedEvent.cs
│   │   ├── ProductSoldEvent.cs
│   │   ├── ProductPriceUpdatedEvent.cs
│   │   └── ReviewPostedEvent.cs
│   ├── ReadModels/
│   │   ├── ProductCatalogueView.cs      ← AverageRating computed from RatingSum / ReviewCount
│   │   └── SellerSummaryView.cs
│   ├── Projections/
│   │   ├── IProjection.cs               ← Apply + Reset
│   │   ├── ProductCatalogueProjection.cs ← handles Listed, Sold, PriceUpdated, Reviewed
│   │   └── SellerSummaryProjection.cs   ← handles Listed, Sold
│   ├── Infrastructure/
│   │   ├── IEventStore.cs / InMemoryEventStore.cs
│   │   └── IReadModelStore.cs / InMemoryReadModelStore.cs
│   ├── Engine/
│   │   └── ProjectionEngine.cs          ← Append + Rebuild
│   ├── Services/
│   │   └── MarketplaceService.cs        ← thin service; converts actions to events
│   └── Program.cs
└── ReadModelPattern.Tests/
    └── ReadModelPatternTests.cs         ← 27 tests across 4 suites
```

## Key Code

### IProjection — two responsibilities

```csharp
public interface IProjection
{
    void Apply(IDomainEvent @event);  // update read model for one event
    void Reset();                     // clear read model for rebuild
}
```

`Apply` is called for every new event in real time. `Reset` is called by `ProjectionEngine.Rebuild()` before replaying — it ensures the rebuilt model is built from a clean slate rather than accumulated on top of stale state.

### ProductCatalogueProjection — selective event handling

```csharp
public void Apply(IDomainEvent @event)
{
    switch (@event)
    {
        case ProductListedEvent e:
            store.Upsert(e.ProductId, new ProductCatalogueView { ... });
            break;
        case ProductSoldEvent e:
            var view = store.Get(e.ProductId);
            view.StockRemaining -= e.Quantity;
            view.TotalSold      += e.Quantity;
            store.Upsert(e.ProductId, view);
            break;
        // ... PriceUpdated, ReviewPosted
    }
}
```

The projection ignores event types it does not care about — `SellerSummaryProjection` receives `ReviewPostedEvent` too but simply skips it. Each projection is a standalone, independently testable unit.

### ProjectionEngine — Append and Rebuild

```csharp
public void Append(IDomainEvent @event)
{
    eventStore.Append(@event);          // persist first
    foreach (var p in _projections)
        p.Apply(@event);                // then fan out
}

public void Rebuild()
{
    foreach (var p in _projections)
        p.Reset();                      // clear all read models
    foreach (var @event in eventStore.GetAll())
        foreach (var p in _projections)
            p.Apply(@event);            // replay from the beginning
}
```

`Rebuild` makes read models disposable. A new projection registered after months of events calls `Rebuild()` once and is fully current. A corrected projection is wiped and replayed without any business transactions being re-executed.

### AverageRating — exact integer arithmetic

```csharp
public sealed class ProductCatalogueView
{
    public int ReviewCount { get; set; }
    public int RatingSum   { get; set; }
    public double AverageRating => ReviewCount > 0 ? (double)RatingSum / ReviewCount : 0.0;
}
```

Storing `RatingSum` and `ReviewCount` separately means `AverageRating` is always derived from exact integers rather than accumulated through floating-point additions. Rebuild produces byte-for-byte identical averages every time.

## Demo Scenarios

```
1. Listing products       — four Canadian products from two sellers entered into the marketplace
2. Recording sales        — five sale events; stock and revenue updated in real time
3. Price update           — one product repriced; catalogue reflects new price immediately
4. Post reviews           — six reviews across three products; average ratings computed
5. Product catalogue view — all products tabulated: price, stock, sold count, avg rating
6. Seller dashboard       — two sellers aggregated: listings, units sold, revenue (CAD)
7. Top selling            — top 2 products ranked by units sold
8. Rebuild demo           — new projection added after events were fired; Rebuild() catches it up
```

## When to Use

- You need multiple query shapes from the same data — a browsing grid, a seller dashboard, a search index, and an analytics report all served without joins or aggregation at query time.
- Query performance is critical and cannot be satisfied by the normalized write model.
- You are using Event Sourcing (4.15) and events are already the primary record; projections are the natural query layer.
- You expect query requirements to change independently of business logic; a new read model can be added and rebuilt without touching the write side.

## When NOT to Use

- You have only one or two simple queries; the overhead of separate read models and projections outweighs the benefit.
- Your domain produces very few events; the entire read model can be recomputed on demand without performance problems.
- Strong read-after-write consistency is required and eventual consistency (the read model may lag briefly) is not acceptable.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Query-optimized shapes | Each read model is tailored to exactly the data one screen or API needs — no joins, no aggregation at query time |
| Independent evolvability | A new projection can be added and rebuilt without changing any write-side code or re-running any business logic |
| Testability | Each projection is a pure function: apply events in a test and assert the resulting read model — no database, no mocks |
| Rebuild from history | Bugs in a projection are fixed and replayed; the event log is the source of truth |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Eventual consistency | A read model may lag slightly behind the write side; callers must tolerate reading data that is milliseconds old |
| Storage duplication | The same data lives in the event store and in each read model store; storage grows with the number of projections |
| Rebuild cost | Replaying a long event history can be slow; production systems use snapshots or batched rebuild strategies |
| Projection management | Each new query shape requires a new projection; large systems can accumulate many of them |

## Related Patterns

- **CQRS (4.03)** — the architectural separation of commands and queries that makes read models natural; projections are the mechanism that populates the query side.
- **Event Sourcing (4.15)** — stores state as events; this pattern builds the query views on top of that event log.
- **Domain Event (4.12)** — domain events are the input that projections consume; the patterns compose directly.
- **Outbox Pattern (4.20)** — ensures events produced by the write side are reliably published to projections even under failures.

## Running the Demo

```bash
cd src/4-Enterprise/4.28-ReadModel/ReadModelPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.28-ReadModel/ReadModelPattern.Tests
dotnet test
```
