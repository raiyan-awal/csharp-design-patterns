# 3.07 — Observer Pattern

## Intent

Define a one-to-many dependency between objects so that when one object (the **Subject**) changes state, all its dependents (**Observers**) are notified and updated automatically.

---

## The Problem It Solves

An `Order` changes status throughout its lifecycle. Multiple systems care about those changes:

```
// WITHOUT Observer — Order has to know about every system:
public void UpdateStatus(OrderStatus newStatus)
{
    _status = newStatus;
    _customerNotifier.SendPush(this);   // Order now depends on CustomerNotifier
    _warehouse.PickOrder(this);         // Order now depends on WarehouseSystem
    _emailService.SendEmail(this);      // Order now depends on EmailService
    // Add a new system? Edit Order. Remove one? Edit Order.
}
```

Every new integration requires modifying the `Order` class. Observer fixes this by inverting the dependency — interested parties register themselves; `Order` doesn't know who they are.

---

## Solution: Order Tracking System

Five observers watch a single order's lifecycle. The order notifies all of them on every status change — without knowing their types.

### Participants

| Role | Class | Responsibility |
|------|-------|---------------|
| **Subject interface** | `IOrderSubject` | `Subscribe` / `Unsubscribe` |
| **Concrete Subject** | `Order` | Maintains observer list; calls `NotifyAll` on `UpdateStatus` |
| **Observer interface** | `IOrderObserver` | `OnOrderUpdated(order, previousStatus)` |
| **Concrete Observers** | `CustomerNotifier`, `WarehouseSystem`, `EmailService`, `AnalyticsDashboard`, `InvoiceService` | React independently to each status change |

### What each observer does

| Observer | Reacts to | Action |
|----------|-----------|--------|
| `CustomerNotifier` | Processing, Shipped, Delivered, Cancelled | Sends push notification |
| `WarehouseSystem` | Processing, Shipped, Delivered, Cancelled | Pick/pack/dispatch/inventory |
| `EmailService` | Placed, Shipped, Delivered, Cancelled | Sends confirmation emails |
| `AnalyticsDashboard` | Every transition | Records `(From, To, DateTime)` |
| `InvoiceService` | Delivered only | Generates invoice |

---

## Structure

```
ObserverPattern/
├── IOrderSubject.cs          ← Subject interface
├── IOrderObserver.cs         ← Observer interface
├── OrderStatus.cs            ← enum: Placed / Processing / Shipped / Delivered / Cancelled
├── Order.cs                  ← Concrete Subject
└── Observers/
    ├── CustomerNotifier.cs
    ├── WarehouseSystem.cs
    ├── EmailService.cs
    ├── AnalyticsDashboard.cs
    └── InvoiceService.cs
```

---

## Key Code

### Subject — notifies all observers on state change

```csharp
public sealed class Order : IOrderSubject
{
    private readonly List<IOrderObserver> _observers = [];
    private OrderStatus _status = OrderStatus.Placed;

    public void Subscribe(IOrderObserver observer)
    {
        if (!_observers.Contains(observer))
            _observers.Add(observer);
    }

    public void Unsubscribe(IOrderObserver observer) => _observers.Remove(observer);

    public void UpdateStatus(OrderStatus newStatus)
    {
        if (newStatus == _status) return;  // no-op guard
        var previous = _status;
        _status = newStatus;
        NotifyAll(previous);
    }

    private void NotifyAll(OrderStatus previous)
    {
        // ToList() snapshot — safe if an observer unsubscribes during notification
        foreach (var observer in _observers.ToList())
            observer.OnOrderUpdated(this, previous);
    }
}
```

### Observer — reacts independently

```csharp
public sealed class InvoiceService : IOrderObserver
{
    public bool     InvoiceGenerated { get; private set; }
    public decimal? InvoiceAmount    { get; private set; }

    public void OnOrderUpdated(Order order, OrderStatus previousStatus)
    {
        if (order.Status == OrderStatus.Delivered)
        {
            InvoiceGenerated = true;
            InvoiceAmount    = order.TotalAmount;
        }
    }
}
```

### Usage

```csharp
var order = new Order("ORD-1001", "CUST-42", 149.99m);

order.Subscribe(new CustomerNotifier());
order.Subscribe(new WarehouseSystem());
order.Subscribe(new InvoiceService());

order.UpdateStatus(OrderStatus.Processing); // → CustomerNotifier + WarehouseSystem fire
order.UpdateStatus(OrderStatus.Shipped);    // → CustomerNotifier + WarehouseSystem fire
order.UpdateStatus(OrderStatus.Delivered);  // → all three fire, invoice generated
```

---

## C# Event Keyword — The Built-in Observer

C#'s `event` keyword IS the Observer pattern baked into the language. Instead of `Subscribe`/`Unsubscribe`, you use `+=` and `-=`:

```csharp
// Manual Observer pattern         │  C# event equivalent
order.Subscribe(observer);         │  order.StatusChanged += handler;
order.Unsubscribe(observer);       │  order.StatusChanged -= handler;
observer.OnOrderUpdated(order, p); │  handler(order, previousStatus);

// With event keyword on the Subject:
public event Action<Order, OrderStatus>? StatusChanged;

private void NotifyAll(OrderStatus previous)
    => StatusChanged?.Invoke(this, previous);
```

**`?.Invoke`** — the null-conditional prevents a `NullReferenceException` when no observers are subscribed (the delegate is null until the first `+=`).

Use the manual pattern (interface + `Subscribe`) when you need to enforce a specific method signature, support multiple methods per observer, or work across assembly boundaries. Use `event` for simple scenarios within a project.

---

## Demo Scenarios

```
DEMO 1 — Full lifecycle: Placed → Processing → Shipped → Delivered
  All 5 observers subscribed; each reacts at its relevant stages

DEMO 2 — Same status twice → observers not notified (guard in UpdateStatus)

DEMO 3 — Unsubscribe CustomerNotifier after Shipped
  → Delivered fires but CustomerNotifier stays silent

DEMO 4 — Late subscriber joins after first status change
  → Only sees events from the point it subscribed

DEMO 5 — Cancellation path: Processing → Cancelled
  → Warehouse returns items to shelf; customer gets cancellation push

DEMO 6 — C# event keyword as the native Observer pattern
```

---

## When to Use

- One object's state change should trigger updates in an unknown number of other objects
- You want to add/remove dependents at runtime without modifying the subject
- An abstraction has two aspects — one dependent on the other — and you want both to vary independently

---

## When NOT to Use

- There are very few, fixed observers — direct method calls are simpler
- Observers need a specific calling order — Observer doesn't guarantee sequence
- The notification chain is long (A notifies B which notifies C…) — hard to debug

---

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Open/Closed** | Add new observers without touching `Order` |
| **Loose coupling** | `Order` only knows `IOrderObserver` — not concrete types |
| **Dynamic** | Subscribe/Unsubscribe at runtime |
| **Single Responsibility** | Each observer handles one concern |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **Unexpected updates** | A change in the Subject cascades to all observers — can be hard to trace |
| **Memory leaks** | Observers that forget to unsubscribe keep the Subject alive (common in C# events) |
| **No guaranteed order** | Observers are notified in subscription order — no way to prioritise |
| **Cascading updates** | Observer A's reaction triggers Subject B, which notifies Observer C… hard to debug |

---

## Observer vs Mediator

| | Observer | Mediator |
|---|---------|---------|
| **Direction** | Subject → Observers (one-to-many, push) | Any device → Hub → Any device (bidirectional) |
| **Who holds logic** | Each observer decides independently what to do | The mediator decides who gets notified and what they should do |
| **Subject awareness** | Subject doesn't know observers | Mediator knows all colleagues |
| **Use when** | Many things should react to one object's events | Many objects need complex, coordinated interaction |

---

## Related Patterns

- **Mediator (3.05)** — similar decoupling; Mediator centralises coordination logic, Observer distributes it
- **Command (3.02)** — observers can be wrapped as commands for queuing or undo
- **Memento (3.06)** — subject can save a memento before notifying, allowing rollback if an observer fails
- **Strategy (3.09)** — observers can be swapped for different reaction strategies

---

## Running the Demo

```bash
cd src/3-Behavioral/3.07-Observer/ObserverPattern
dotnet run
```

## Running the Tests

```bash
cd src/3-Behavioral/3.07-Observer/ObserverPattern.Tests
dotnet test
```
