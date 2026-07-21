# 3.12 — Null Object Pattern

## Intent

Provide a do-nothing default object that implements the same interface as a real dependency. Callers inject it when behaviour is not needed, eliminating null checks from the consuming code entirely.

---

## The Problem It Solves

Optional dependencies handled with `null` force every method that uses them to check before calling:

```csharp
// WITHOUT Null Object — null checks multiply across every method:
public sealed class OrderService
{
    private readonly ICustomerNotifier? _notifier;
    private readonly IAuditLogger?      _logger;

    public void PlaceOrder(Order order)
    {
        if (_logger   != null) _logger.Log(order.OrderId, "PlaceOrder");
        if (_notifier != null) _notifier.NotifyOrderPlaced(order);
    }

    public void ShipOrder(Order order, string tracking)
    {
        if (_logger   != null) _logger.Log(order.OrderId, "ShipOrder");
        if (_notifier != null) _notifier.NotifyOrderShipped(order, tracking);
    }

    // Every method repeats the same pair of null guards.
    // Each guard is also a branch that unit tests must cover twice.
}
```

Every new method doubles the null-check burden. Tests must cover both branches (dependency set / not set) in every method. The class is noisier than it needs to be.

---

## Solution: Null Implementations

Two interfaces cover the optional dependencies. Each has a real implementation and a do-nothing Null implementation. `OrderService` accepts non-nullable parameters; callers supply the Null version when the behaviour is not wanted.

### Participants

| Role | Class | Responsibility |
|------|-------|---------------|
| **Abstraction** | `ICustomerNotifier` | Notification contract |
| **Real impl** | `EmailNotifier`, `SmsNotifier` | Send actual messages |
| **Null impl** | `NullCustomerNotifier` | Silent no-ops for all methods |
| **Abstraction** | `IAuditLogger` | Audit logging contract |
| **Real impl** | `ConsoleAuditLogger` | Write timestamped audit entries |
| **Null impl** | `NullAuditLogger` | Silent no-op |
| **Context** | `OrderService` | Uses both — zero null checks |

---

## Structure

```
NullObjectPattern/
├── ICustomerNotifier.cs       ← Notifier abstraction
├── IAuditLogger.cs            ← Logger abstraction
├── Models/
│   └── Order.cs
├── Notifiers/
│   ├── EmailNotifier.cs       ← Real: sends emails
│   ├── SmsNotifier.cs         ← Real: sends SMS
│   └── NullCustomerNotifier.cs ← Null: all methods are no-ops
├── Loggers/
│   ├── ConsoleAuditLogger.cs  ← Real: timestamped console entries
│   └── NullAuditLogger.cs     ← Null: Log() does nothing
└── OrderService.cs            ← Context: no null checks, always calls
```

---

## Key Code

### Null Object — all methods are empty

```csharp
public sealed class NullCustomerNotifier : ICustomerNotifier
{
    public static readonly NullCustomerNotifier Instance = new();
    private NullCustomerNotifier() { }

    public void NotifyOrderPlaced(Order order)                         { }
    public void NotifyOrderShipped(Order order, string trackingNumber) { }
    public void NotifyOrderCancelled(Order order, string reason)       { }
    public void NotifyRefundIssued(Order order, decimal amount)        { }
}
```

### Context — non-nullable parameters, zero null checks

```csharp
public sealed class OrderService
{
    private readonly ICustomerNotifier _notifier;
    private readonly IAuditLogger      _logger;

    public OrderService(ICustomerNotifier notifier, IAuditLogger logger)
    {
        _notifier = notifier;
        _logger   = logger;
    }

    public void PlaceOrder(Order order)
    {
        order.UpdateStatus("Confirmed");
        _logger.Log(order.OrderId, "PlaceOrder", $"${order.Total:F2}");  // always called
        _notifier.NotifyOrderPlaced(order);                               // always called
    }
}
```

### Usage — caller decides what to inject

```csharp
// Full setup: emails + audit trail
var svc1 = new OrderService(new EmailNotifier(), new ConsoleAuditLogger());

// SMS only, no audit logging
var svc2 = new OrderService(new SmsNotifier(), NullAuditLogger.Instance);

// Audit logging only, no customer notifications
var svc3 = new OrderService(NullCustomerNotifier.Instance, new ConsoleAuditLogger());

// Fully silent (tests, batch jobs, dry-run mode)
var svc4 = new OrderService(NullCustomerNotifier.Instance, NullAuditLogger.Instance);
```

All four `OrderService` instances are created and used identically — the same method calls, the same code paths, no conditional logic anywhere.

---

## Demo Scenarios

```
PROBLEM   — shows the null-check version and why it hurts
DEMO 1    — full setup: EmailNotifier + ConsoleAuditLogger
DEMO 2    — SMS notifications, NullAuditLogger (no audit trail)
DEMO 3    — ConsoleAuditLogger, NullCustomerNotifier (no notifications)
DEMO 4    — both Null Objects: silent mode for batch / test scenarios
DEMO 5    — the key insight: OrderService source has zero null checks
```

---

## When to Use

- A dependency is optional but used in many places — null checks would scatter everywhere
- You want to make an optional dependency explicit at the call site (injected) rather than conditional at the use site
- You want unit tests to focus on business logic, not on "what if this dependency is null"

---

## When NOT to Use

- The absent case is rare and isolated to one place — a simple null check there is cleaner
- You need to distinguish "dependency not set" from "dependency present but did nothing" — Null Object hides that distinction
- The interface is large and complex; maintaining an accurate no-op for every method becomes tedious (consider a mock framework instead)

---

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Eliminates null checks** | The consuming class never checks — a valid object is always present |
| **Simpler code paths** | Every code path through the consumer is identical — nothing to test twice |
| **Explicit at call site** | Injecting `NullAuditLogger.Instance` makes the "no logging" intent visible |
| **Open/Closed** | Add a new notifier type without touching `OrderService` at all |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **Hides intent** | Callers may forget they injected a null object and wonder why nothing happened |
| **Interface maintenance** | Every new method on the interface must be added to the Null Object too |
| **Not a substitute for errors** | If a dependency being absent is an error, null-checking and throwing is more honest |

---

## Null Object vs Strategy

Both involve interchangeable implementations behind an interface.

| | Null Object | Strategy |
|---|-------------|---------|
| **Purpose** | Eliminate null checks | Swap algorithms |
| **Null impl behaviour** | Always does nothing | Not applicable |
| **Chosen by** | The caller at injection time | The caller or context at runtime |
| **Number of implementations** | Real + one Null | Many real variants |

---

## Related Patterns

- **Strategy (3.09)** — Null Object is essentially a Strategy whose behaviour is "do nothing"
- **Decorator (2.4)** — Decorator wraps and adds behaviour; Null Object wraps and removes it
- **Singleton (1.1)** — Null Objects are often singletons (stateless, one instance is enough)
- **Proxy (2.7)** — Proxy controls access; Null Object provides a safe stand-in when there is nothing to access

---

## Running the Demo

```bash
cd src/3-Behavioral/3.12-NullObject/NullObjectPattern
dotnet run
```

## Running the Tests

```bash
cd src/3-Behavioral/3.12-NullObject/NullObjectPattern.Tests
dotnet test
```
