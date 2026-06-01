# 2.1 Adapter Pattern

## Intent

Convert the interface of a class into another interface that clients expect. Adapter lets classes work together that couldn't otherwise because of incompatible interfaces.

---

## The Problem It Solves

Your application defines a clean internal interface (`IPaymentProcessor`). You then need to integrate three third-party payment SDKs — Stripe, PayPal, and Square — each with completely different method signatures, argument types, and return types. You cannot modify those SDKs.

Without the Adapter pattern, you'd scatter SDK-specific translation code across your application. With it, each SDK gets one adapter class that absorbs all translation, and your application never knows the difference.

---

## Domain: Payment Processing

| Role | Class | Description |
|------|-------|-------------|
| **Target Interface** | `IPaymentProcessor` | The interface your application depends on |
| **Client** | `OrderService` | Uses only `IPaymentProcessor` — no SDK knowledge |
| **Adaptee 1** | `StripeGateway` | Third-party SDK: uses decimal dollars, returns `StripeCharge` |
| **Adaptee 2** | `PayPalSdk` | Third-party SDK: uses string amounts, requires request objects |
| **Adaptee 3** | `SquareApi` | Third-party SDK: uses `Money` struct, returns `ApiResponse<T>` |
| **Adapter 1** | `StripeAdapter` | Wraps `StripeGateway` → translates to `IPaymentProcessor` |
| **Adapter 2** | `PayPalAdapter` | Wraps `PayPalSdk` → translates to `IPaymentProcessor` |
| **Adapter 3** | `SquareAdapter` | Wraps `SquareApi` → translates to `IPaymentProcessor` |
| **Result VO** | `PaymentResult` | Uniform outcome type — Success/Fail with transaction ID |

---

## Structure

```
IPaymentProcessor          ← Target interface (your code depends on this)
    ↑
StripeAdapter              ← Adapter: wraps StripeGateway
PayPalAdapter              ← Adapter: wraps PayPalSdk
SquareAdapter              ← Adapter: wraps SquareApi

OrderService               ← Client: only knows IPaymentProcessor
```

---

## Key Translations Each Adapter Performs

### StripeAdapter
| Our Interface | Stripe SDK |
|---|---|
| `amountInCents` (int) | `amount` (decimal dollars) ÷ 100 |
| `PaymentResult.Ok(...)` | `StripeCharge { Paid = true }` |
| `PaymentResult.Fail(...)` | `StripeCharge { Paid = false }` |

### PayPalAdapter
| Our Interface | PayPal SDK |
|---|---|
| `cardToken + amount + currency` (flat params) | `PayPalPaymentOrder` (request object) |
| `amountInCents` (int) | string `"49.99"` ÷ 100 formatted |
| `Refund(...)` | `ReverseCapture(...)` |

### SquareAdapter
| Our Interface | Square SDK |
|---|---|
| `amountInCents + currency` (separate) | `Money` struct (combined) |
| `cardToken` | `sourceId` |
| `Charge(...)` | `CreatePayment(sourceId, money, idempotencyKey)` |
| `ApiResponse<T>` | `PaymentResult` |

---

## When to Use

- You need to use a class whose interface is incompatible with what your application expects
- You want to integrate multiple third-party libraries behind a unified interface
- You're wrapping a legacy system and don't want legacy details leaking into new code
- You want to make your application independent of specific vendor APIs

## When NOT to Use

- The third-party class has an acceptable interface already — wrapping adds no value
- You control both sides of the integration — refactor the interface instead
- The incompatibility is so deep that the adapter becomes a full reimplementation

---

## Object Adapter vs Class Adapter

This implementation uses the **Object Adapter** (composition):

```csharp
// Object Adapter — wraps via composition (recommended in C#)
public sealed class StripeAdapter : IPaymentProcessor
{
    private readonly StripeGateway _stripe;  // holds a reference
    ...
}
```

A **Class Adapter** uses multiple inheritance (inherits from both target and adaptee). C# doesn't support multiple class inheritance, so the closest equivalent is to inherit from the adaptee and implement the target interface:

```csharp
// Class Adapter — inherits the adaptee, implements the target interface
// Only possible when you can subclass the adaptee (i.e. it's not sealed).
public class StripeClassAdapter : StripeGateway, IPaymentProcessor
{
    public StripeClassAdapter(string secretKey) : base(secretKey) { }

    public string ProcessorName => "Stripe";

    public PaymentResult Charge(string cardToken, int amountInCents, string currency)
    {
        // Calls the inherited StripeGateway method directly — no stored reference needed.
        StripeCharge charge = CreateCharge(cardToken, amountInCents / 100m, currency);
        return charge.Paid
            ? PaymentResult.Ok(charge.Id, $"Stripe charged {currency} {amountInCents / 100m:F2}")
            : PaymentResult.Fail($"Stripe declined: {charge.FailureMessage}");
    }

    public PaymentResult Refund(string transactionId, int amountInCents)
    {
        StripeRefund refund = CreateRefund(transactionId, amountInCents / 100m);
        return refund.Status == "succeeded"
            ? PaymentResult.Ok(refund.Id, $"Stripe refunded {amountInCents / 100m:F2}")
            : PaymentResult.Fail($"Stripe refund failed: {refund.Reason}");
    }
}
```

**Why the object adapter is almost always preferred:**
- Most third-party SDK classes are `sealed` — you can't subclass them
- Inheritance exposes all of the adaptee's public methods to callers, leaking the incompatible interface
- Composition is easier to test (you can swap in a fake `StripeGateway`) and easier to reason about

---

## Benefits

- **Open/Closed Principle**: add new payment gateways by adding a new adapter — existing code untouched
- **Single Responsibility**: each adapter handles exactly one translation concern
- **Testability**: `OrderService` can be tested with any `IPaymentProcessor` mock
- **Flexibility**: swap gateways at runtime (A/B testing, regional routing, failover)

## Drawbacks

- More classes — one adapter per adaptee
- Adapter can become complex if the adaptee interface is very different from the target
- Thin adapters may feel like boilerplate — but they're valuable as an explicit translation layer

---

## Running the Demo

```bash
cd src/2-Structural/2.1-Adapter/AdapterPattern
dotnet run
```

## Running Tests

```bash
cd src/2-Structural/2.1-Adapter/AdapterPattern.Tests
dotnet test
```

---

## Related Patterns

- **Facade** — also wraps a complex subsystem, but unifies multiple interfaces rather than translating one
- **Decorator** — wraps an object to add behaviour; Adapter wraps to change the interface
- **Proxy** — wraps an object with the same interface; Adapter wraps with a *different* interface

All four patterns wrap something, but the *intent* and *interface relationship* differ:

### Adapter — changes the interface

You have `StripeGateway.CreateCharge(token, decimal, currency)` but your app needs `IPaymentProcessor.Charge(token, int, currency)`. The adapter translates one into the other. The wrapped object's interface is **incompatible** with what the caller expects.

```csharp
// Caller expects IPaymentProcessor — StripeGateway doesn't implement it.
// Adapter bridges the gap.
IPaymentProcessor processor = new StripeAdapter(new StripeGateway("key"));
processor.Charge("tok_visa", 4999, "USD");
```

### Facade — simplifies many interfaces into one

You have an `OrderValidator`, `InventoryService`, `PaymentGateway`, and `EmailSender` — all separate, all complex. A Facade provides a single `CheckoutService.PlaceOrder(...)` that orchestrates all of them. There's no incompatibility to fix; the goal is simplification.

```csharp
// No interface mismatch — just hiding complexity behind one entry point.
var checkout = new CheckoutFacade(validator, inventory, payment, email);
checkout.PlaceOrder(cart);   // coordinates 4 subsystems internally
```

### Decorator — same interface, adds behaviour

You have `IPaymentProcessor` and want to add retry logic on top. `RetryDecorator` implements `IPaymentProcessor`, wraps another `IPaymentProcessor`, and adds retries before delegating. The interface **stays the same** — the caller can't tell there's a decorator.

```csharp
// Same interface in and out — just layering behaviour.
IPaymentProcessor processor = new RetryDecorator(
    new StripeAdapter(new StripeGateway("key")),
    maxRetries: 3);
processor.Charge("tok_visa", 4999, "USD");   // retries automatically on failure
```

### Proxy — same interface, controls access

You have `IPaymentProcessor` and want to add logging or authorisation. `LoggingProxy` implements `IPaymentProcessor`, forwards calls to the real processor, and logs each one. Again the **interface is the same** — but unlike Decorator (which adds behaviour), a Proxy typically controls *whether or how* the call reaches the real object.

```csharp
// Same interface — proxy decides whether to let the call through.
IPaymentProcessor processor = new LoggingProxy(
    new StripeAdapter(new StripeGateway("key")),
    logger);
processor.Charge("tok_visa", 4999, "USD");   // logs then forwards
```

### Quick comparison

| Pattern | Interface changes? | Wraps | Goal |
|---|---|---|---|
| **Adapter** | Yes — target ≠ adaptee | One incompatible object | Make it fit your interface |
| **Facade** | N/A — new interface | Multiple subsystems | Simplify a complex API |
| **Decorator** | No — same in and out | One compatible object | Add behaviour transparently |
| **Proxy** | No — same in and out | One compatible object | Control access |
