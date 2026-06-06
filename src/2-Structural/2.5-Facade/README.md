# 2.5 Facade Pattern

## Intent

Provide a unified interface to a set of interfaces in a subsystem. Facade defines a higher-level interface that makes the subsystem easier to use.

---

## The Problem It Solves

Placing an order involves five separate subsystems — each with its own API, its own call sequence, and its own failure modes. Without a Facade, every caller must know all of this:

```csharp
// Without Facade — duplicated in every controller, job, or service:
foreach (var line in order.Lines)
    if (!inventory.IsAvailable(line.ProductId, line.Quantity))
        return error;

foreach (var line in order.Lines)
    inventory.Reserve(line.ProductId, line.Quantity);

var charge = payment.Charge(order.CardToken, total);
if (!charge.Success)
{
    foreach (var line in order.Lines)
        inventory.Release(line.ProductId, line.Quantity);  // ← compensation
    return error;
}

var tracking = shipping.CreateShipment(address, lines);
notifications.SendOrderConfirmation(email, orderId, tracking);
audit.Log("PlaceOrder", ...);
```

With Facade, all of that becomes one call:

```csharp
var result = orderFacade.PlaceOrder(order);
```

---

## Domain: Order Processing

| Role | Class | Description |
|------|-------|-------------|
| **Facade** | `OrderFacade` | `PlaceOrder()` and `CancelOrder()` — the only API clients need |
| **Subsystem** | `InventoryService` | Stock availability, reservation, release |
| **Subsystem** | `PaymentService` | Card charging and refunds |
| **Subsystem** | `ShippingService` | Shipment creation and cancellation |
| **Subsystem** | `NotificationService` | Customer email confirmations |
| **Subsystem** | `AuditLogger` | Tamper-evident action log |

---

## Structure

```
Client
  │
  └──▶ OrderFacade
            │
            ├──▶ InventoryService   (checks stock, reserves, releases)
            ├──▶ PaymentService     (charges, refunds)
            ├──▶ ShippingService    (creates/cancels shipments)
            ├──▶ NotificationService (sends emails)
            └──▶ AuditLogger        (records all actions)
```

The client knows only `OrderFacade`. It has no reference to any subsystem. Subsystems can change their internal APIs without any caller updating.

---

## Key Feature: Compensation on Failure

The Facade encapsulates the rollback logic that would otherwise be duplicated at every call site. If payment is declined after stock has already been reserved, the Facade releases that stock automatically:

```csharp
// Inside OrderFacade.PlaceOrder():
foreach (var line in order.Lines)
    _inventory.Reserve(line.ProductId, line.Quantity);   // step 2

var payment = _payment.Charge(order.CardToken, total);   // step 3

if (!payment.Success)
{
    // Compensate — release the stock we just reserved
    foreach (var line in order.Lines)
        _inventory.Release(line.ProductId, line.Quantity);
    return OrderResult.Fail(order.OrderId, $"Payment failed: {payment.Message}");
}
```

Without the Facade, every caller would need to implement this compensation themselves. One missed release = a stock leak in production.

---

## Subsystems Remain Accessible

The Facade does not hide the subsystems — it simplifies access to them. Advanced callers can still reach subsystems directly when they need to:

```csharp
// Most callers:
var result = facade.PlaceOrder(order);

// Advanced use — query inventory directly without going through the Facade:
int stock = inventoryService.GetStock("LAPTOP-001");
```

This is an important distinction: Facade simplifies, it does not restrict.

---

## When to Use

- A subsystem is complex and clients only need a subset of its capabilities
- You want to layer your system — a high-level facade over lower-level subsystems
- You want to reduce coupling between clients and subsystem internals
- Multiple callers need the same sequence of subsystem calls

## When NOT to Use

- Clients genuinely need fine-grained control over individual subsystem steps
- The subsystem is simple enough that a facade adds no value
- The "facade" would just be a thin wrapper with no logic — skip it

---

## Benefits

- Reduces coupling between clients and complex subsystem internals
- Encapsulates sequencing, compensation, and coordination in one place
- Subsystems can evolve independently without breaking clients
- Makes the common case trivially simple; the uncommon case still possible

## Drawbacks

- Can become a "God class" if too many unrelated operations are added to it
- Clients may not know which subsystem to go to for advanced operations
- A leaky Facade (one that exposes subsystem types in its return values) defeats the purpose

---

## Running the Demo

```bash
cd src/2-Structural/2.5-Facade/FacadePattern
dotnet run
```

## Running Tests

```bash
cd src/2-Structural/2.5-Facade/FacadePattern.Tests
dotnet test
```

---

## Related Patterns

- **Adapter** — converts an existing interface into a different one; Facade defines a new simplified interface over several existing ones
- **Mediator** — also centralises coordination, but between objects that know each other; Facade clients don't know the subsystems exist
- **Abstract Factory** — can be used with Facade to provide a single entry point for creating the subsystem objects themselves
- **Decorator** — adds behaviour to a single object; Facade simplifies access to many objects

---

### Facade vs Adapter

```csharp
// Adapter — wraps ONE existing class to match a target interface:
public sealed class StripeAdapter : IPaymentProcessor
{
    private readonly StripeGateway _stripe;  // one adaptee
    public PaymentResult Charge(...) => translate(_stripe.CreateCharge(...));
}

// Facade — wraps MULTIPLE subsystems behind a new, simpler interface:
public sealed class OrderFacade
{
    // Five subsystems — none of their APIs change
    public OrderResult PlaceOrder(Order order)  // new higher-level operation
    {
        _inventory.Reserve(...);
        _payment.Charge(...);
        _shipping.CreateShipment(...);
        _notifications.Send(...);
        _audit.Log(...);
    }
}
```

Adapter is about interface translation (one-to-one). Facade is about simplification (many-to-one).

---

### Facade vs Mediator

Both centralise coordination, but from different angles:

```csharp
// Mediator — subsystems know the mediator exists and talk through it:
public sealed class OrderMediator
{
    public void Notify(object sender, string eventName)
    {
        if (sender is InventoryService && eventName == "Reserved")
            _payment.Charge(...);   // subsystem triggered another
    }
}
// InventoryService calls: _mediator.Notify(this, "Reserved");

// Facade — subsystems are completely unaware of each other and the Facade:
public sealed class OrderFacade
{
    public OrderResult PlaceOrder(Order order)
    {
        _inventory.Reserve(...);   // InventoryService has no idea about Payment
        _payment.Charge(...);      // PaymentService has no idea about Shipping
    }
}
```

Mediator: subsystems → mediator → subsystems (bidirectional). Facade: client → facade → subsystems (one-directional). Subsystems in a Facade don't know they're being coordinated.
