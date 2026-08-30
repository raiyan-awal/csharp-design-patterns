# 4.20 — Outbox Pattern

## Intent

The Outbox Pattern guarantees that a database write and a message publication happen atomically, without a distributed transaction. Instead of publishing events directly to a message broker (which could succeed or fail independently of the database), you write both the domain record and the outgoing event into the **same database transaction**. A background relay process then reads the undelivered events from the outbox table and publishes them to the broker, retrying on failure.

## The Problem It Solves

Consider placing an order that must also notify downstream services:

```csharp
// Without the Outbox Pattern
orderRepository.Save(order);          // ← DB write succeeds
eventBus.Publish(new OrderPlaced());  // ← broker call fails — event lost forever
```

Or worse, in the opposite order:

```csharp
eventBus.Publish(new OrderPlaced());  // ← broker call succeeds — event sent
orderRepository.Save(order);          // ← DB write fails — order never existed
```

Problems:
- A crash or network hiccup between the two operations leaves the system in an inconsistent state.
- You cannot wrap a database write and a broker publish in the same ACID transaction — they are different systems.
- Retrying naively can publish the event twice, causing duplicate processing downstream.

## Solution: Write to the Outbox, Relay Asynchronously

```csharp
// OrderService — both writes in the same DB transaction
orderRepository.Save(order);
outboxStore.Add(new OutboxMessage { EventType = "OrderPlaced", Payload = Serialize(order) });

// Separate relay process — runs on a schedule (e.g. every 5 seconds)
foreach (var message in outboxStore.GetUnprocessed())
{
    broker.Publish(message);          // only called once the event is durably stored
    outboxStore.MarkProcessed(message.Id);
}
```

If the relay publish fails, the message stays unprocessed in the outbox and is retried on the next relay run.

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Outbox message | `OutboxMessage` | Stores event type, payload, and processed timestamp |
| Outbox store | `IOutboxStore` / `InMemoryOutboxStore` | Persists outbox entries; exposes unprocessed messages |
| Relay | `OutboxRelay` | Reads unprocessed messages, publishes them, marks processed; retries on failure |
| Domain service | `OrderService` | Saves the order and writes to the outbox in the same logical transaction |
| Repository | `IOrderRepository` / `InMemoryOrderRepository` | Stores domain orders |
| Event handlers | `SimulatedEmailHandler`, `SimulatedInventoryHandler` | Simulate downstream subscribers notified by the relay |

## Structure

```
src/4-Enterprise/4.20-OutboxPattern/
├── OutboxPattern/
│   ├── Core/
│   │   ├── OutboxMessage.cs          ← outbox record (EventType, Payload, ProcessedAt)
│   │   ├── IOutboxStore.cs           ← Add / GetUnprocessed / MarkProcessed
│   │   ├── InMemoryOutboxStore.cs    ← thread-safe in-memory store
│   │   └── OutboxRelay.cs            ← reads unprocessed, publishes, marks done
│   ├── Domain/
│   │   ├── Order.cs
│   │   └── OrderItem.cs
│   ├── Infrastructure/
│   │   ├── IOrderRepository.cs
│   │   └── InMemoryOrderRepository.cs
│   ├── Services/
│   │   ├── OrderService.cs           ← atomic: save order + write outbox
│   │   └── SimulatedEventHandlers.cs ← email and inventory subscribers
│   └── Program.cs
└── OutboxPattern.Tests/
    └── OutboxPatternTests.cs         ← 20 tests across 5 suites
```

## Key Code

### OutboxMessage — the durable event record

```csharp
public sealed class OutboxMessage
{
    public Guid      Id             { get; init; } = Guid.NewGuid();
    public string    EventType      { get; init; } = "";
    public string    Payload        { get; init; } = "";
    public DateTime  CreatedAtUtc   { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public bool      IsProcessed    => ProcessedAtUtc.HasValue;
}
```

`ProcessedAtUtc` is mutable — only this field changes after creation. `IsProcessed` is a computed property so there is no way for the two to disagree.

### OrderService — the atomic double-write

```csharp
public Order PlaceOrder(string customerId, string customerName, IEnumerable<OrderItem> items)
{
    var order = new Order { CustomerId = customerId, CustomerName = customerName, Items = [.. items] };

    // In a real application, both writes happen inside a single database transaction.
    // If the transaction rolls back, both the order row and the outbox row disappear together.
    orderRepo.Save(order);
    outboxStore.Add(new OutboxMessage { EventType = "OrderPlaced", Payload = Serialize(order) });

    return order;
}
```

If `orderRepo.Save` throws, `outboxStore.Add` is never reached — no orphaned event. If the outbox write fails, the caller catches the exception and the order is not committed either (in a real DB with a transaction).

### OutboxRelay — publish and retry

```csharp
public int ProcessPending()
{
    var processed = 0;
    foreach (var message in outboxStore.GetUnprocessed())
    {
        try
        {
            publish(message);                    // send to broker or fan-out to handlers
            outboxStore.MarkProcessed(message.Id);
            onPublished?.Invoke(message.EventType);
            processed++;
        }
        catch { /* leave unprocessed; relay retries on next run */ }
    }
    return processed;
}
```

A failing publish leaves `ProcessedAtUtc` null, so the message appears in `GetUnprocessed()` again on the next relay run. `processed` is returned so callers can observe the batch count. The relay processes each message independently — one failure does not block others in the same run.

### IOutboxStore — the three-method contract

```csharp
public interface IOutboxStore
{
    void Add(OutboxMessage message);
    IReadOnlyList<OutboxMessage> GetUnprocessed();
    void MarkProcessed(Guid messageId);
}
```

`MarkProcessed` accepts only the `Id` rather than the full message object, so callers hold the minimum reference needed and there is no way to accidentally modify a message through this call.

## Demo Scenarios

```
=== Maple Shop — Outbox Pattern Demo ===

--- Section 1: Normal Order Flow ---
  Order placed  : <guid>
  Customer      : Alice Tremblay
  Total         : $52.47 CAD
  Outbox pending: 1
  [Running relay...]
  ✓ Published: OrderPlaced
  Relay processed  : 1 message(s)
  Outbox pending   : 0

--- Section 2: Broker Failure — Retry on Next Run ---
  Outbox pending: 1
  [First relay run — inventory service unavailable...]
  Processed     : 0 (0 expected — publish threw)
  Outbox pending: 1 (message still queued)
  [Second relay run — inventory service recovered...]
  ✓ Published: OrderPlaced
  Processed     : 1
  Outbox pending: 0

--- Section 3: Atomicity — No Orphaned Outbox Entry on Failure ---
  [Simulating database write failure...]
  ✗ Order save failed: Database write failed.
  Outbox pending before: 0
  Outbox pending after : 0
  ✓ No orphaned outbox entry — atomicity preserved.

--- Section 4: Batch Processing — Multiple Orders ---
  Orders placed : 3
  Outbox pending: 3
  [Running relay...]
  ✓ Published: OrderPlaced (×3)
  Relay processed: 3 message(s)
  Outbox pending : 0
```

## When to Use

- A service must update its own database AND publish an event or message to another system — and both must succeed or both must fail.
- You need at-least-once delivery of events with the ability to retry without losing messages.
- You want to decouple the moment an event is generated (inside the business transaction) from the moment it is delivered (by the relay).
- Your downstream services are idempotent and can safely handle the occasional duplicate delivery.

## When NOT to Use

- You need exactly-once delivery end-to-end — the Outbox gives at-least-once; the receiving service must handle duplicates with idempotency keys.
- The publishing delay introduced by the relay (typically milliseconds to seconds) is unacceptable for your use case — for true real-time needs, consider a synchronous call with a compensating saga.
- All data lives in a single database and you can express both operations as one transaction — no outbox needed.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Atomic consistency | The order record and the outbox entry are written in the same transaction — no split-brain. |
| Reliable delivery | The relay retries as many times as needed; events are never silently dropped. |
| Broker independence | The relay can target any broker (RabbitMQ, Azure Service Bus, Kafka) or call any downstream service — the domain service does not care. |
| Observable | `GetUnprocessed().Count` exposes pending event backlog for health dashboards and alerting. |
| Testable | `IOutboxStore` is an interface; `OutboxRelay` takes `Action<OutboxMessage>` — both are trivially stubbable in tests without a real broker. |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| At-least-once delivery | If the relay publishes but crashes before calling `MarkProcessed`, the message is retried — receivers must be idempotent. |
| Polling overhead | The relay continuously polls the outbox table; at high throughput this adds DB load, mitigated by a short poll interval and indexed queries on the unprocessed flag. |
| Extra table | Requires an outbox table (or collection) in the primary datastore alongside the domain tables. |
| Delivery lag | Events are not published instantly — the relay introduces a delay (typically under a second in production setups). |

## Related Patterns

- **Saga Pattern (4.19)** — Sagas generate compensating-transaction commands that must be delivered reliably; the Outbox ensures those commands are not lost if the process crashes mid-saga.
- **Inbox Pattern (4.26)** — The Outbox solves reliable *sending*; the Inbox solves reliable *receiving* — recording incoming messages before processing so a crash does not cause a message to vanish.
- **Retry Pattern (4.17)** — Wrap each relay publish call in a `RetryPolicy` to automatically handle transient broker failures before giving up and leaving the message for the next relay run.
- **Domain Event (4.12)** — Domain events raised inside aggregates are the natural candidates to be serialised into outbox messages, keeping the aggregate pure while the outbox handles delivery.

## Running the Demo

```bash
cd src/4-Enterprise/4.20-OutboxPattern/OutboxPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.20-OutboxPattern/OutboxPattern.Tests
dotnet test
```
