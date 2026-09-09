# 1.2 — Factory Method

## Intent

Define an interface for creating an object but let subclasses decide which class to instantiate, so object creation is decoupled from object use and new product types can be added without modifying existing code.

## The Problem It Solves

Without a factory, every creation site must switch on the payment type and reference every concrete class directly:

```csharp
// Without Factory Method: a switch in every creation site
IPaymentProcessor CreateProcessor(string type, Dictionary<string, string> cfg) => type switch
{
    "creditcard" => new CreditCardProcessor(cfg["cardNumber"], cfg["cardHolderName"], cfg["expiryDate"]),
    "paypal"     => new PayPalProcessor(cfg["email"], cfg["apiKey"]),
    "crypto"     => new CryptoProcessor(cfg["walletAddress"], cfg["cryptoCurrency"]),
    _            => throw new ArgumentException($"Unknown type: {type}")
};
```

Problems this creates:
- Adding a new payment type requires modifying this switch — and every other switch like it scattered across the codebase.
- The calling code is coupled to all concrete processor classes at compile time.
- Validation and per-type workflow differences bleed out of the creation site into general application code.
- Unit tests must mock or instantiate concrete processors they should not know about.

## Solution: Abstract creator + factory method hook

Each concrete factory subclass implements one method (`CreateProcessor`) that returns the right product type. The abstract base class defines the standard payment workflow using whichever processor the subclass produces — callers interact only with the abstract factory and the product interface.

```csharp
// Client only knows about the abstract factory — no concrete types in sight:
PaymentProcessorFactory factory = new CreditCardProcessorFactory(
    cardNumber: "4111-1111-1111-1111",
    cardHolderName: "Jane Doe",
    expiryDate: "12/28");

string transactionId = factory.ProcessTransaction(149.99m);
```

To add a new payment method, write a new `PaymentProcessorFactory` subclass — nothing else changes.

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Product interface | `IPaymentProcessor` | Contract all payment processors must fulfil (validate, process, get name) |
| Concrete product | `CreditCardProcessor` | Luhn validation, masked card display, PAN-based transaction ID |
| Concrete product | `PayPalProcessor` | Email-format validation, PayPal-style transaction ID |
| Concrete product | `CryptoProcessor` | Wallet-address validation, blockchain-style transaction ID |
| Abstract creator | `PaymentProcessorFactory` | Declares `CreateProcessor()` hook; implements the standard `ProcessTransaction` workflow |
| Concrete creator | `CreditCardProcessorFactory` | Returns a `CreditCardProcessor`; receives card credentials in its constructor |
| Concrete creator | `PayPalProcessorFactory` | Returns a `PayPalProcessor`; receives email and API key |
| Concrete creator | `CryptoProcessorFactory` | Returns a `CryptoProcessor`; receives wallet address and currency |
| Simple Factory (comparison) | `SimplePaymentProcessorFactory` | Switch-based alternative shown to contrast with the pattern — violates Open/Closed |

## Structure

```
1.2-FactoryMethod/
├── FactoryMethodPattern/
│   ├── IPaymentProcessor.cs        ← product interface
│   ├── PaymentProcessors.cs        ← CreditCardProcessor, PayPalProcessor, CryptoProcessor
│   ├── PaymentProcessorFactory.cs  ← abstract creator + 3 concrete creators + SimplePaymentProcessorFactory
│   └── Program.cs
└── FactoryMethodPattern.Tests/
    └── FactoryMethodPatternTests.cs
```

## Key Code

### Abstract creator with factory method hook

```csharp
public abstract class PaymentProcessorFactory
{
    // Factory method — subclasses decide what to return
    protected abstract IPaymentProcessor CreateProcessor();

    // Template method — standard workflow, agnostic of which processor is used
    public string ProcessTransaction(decimal amount)
    {
        var processor = CreateProcessor();    // polymorphic dispatch
        processor.ValidatePaymentMethod();
        var txId = processor.ProcessPayment(amount);
        LogTransaction(processor.GetProcessorName(), amount, txId);
        return txId;
    }
}
```

### Minimal concrete creator

```csharp
public class CreditCardProcessorFactory : PaymentProcessorFactory
{
    private readonly string _cardNumber, _cardHolderName, _expiryDate;

    public CreditCardProcessorFactory(string cardNumber, string cardHolderName, string expiryDate)
        => (_cardNumber, _cardHolderName, _expiryDate) = (cardNumber, cardHolderName, expiryDate);

    protected override IPaymentProcessor CreateProcessor()
        => new CreditCardProcessor(_cardNumber, _cardHolderName, _expiryDate);
}
```

The concrete creator exists solely to implement `CreateProcessor()` — all workflow logic stays in the base class.

### Simple Factory contrast

```csharp
// SimplePaymentProcessorFactory — NOT the Factory Method pattern
public static IPaymentProcessor CreateProcessor(string type, Dictionary<string, string> cfg)
    => type.ToLower() switch
    {
        "creditcard" => new CreditCardProcessor(cfg["cardNumber"], ...),
        "paypal"     => new PayPalProcessor(cfg["email"], ...),
        "crypto"     => new CryptoProcessor(cfg["walletAddress"], ...),
        _            => throw new ArgumentException($"Unknown: {type}")
    };
```

This is simpler, but every new payment type requires opening and modifying the switch. Factory Method avoids that by pushing creation into subclasses.

## Demo Scenarios

```
1. Credit card processing     — CreditCardProcessorFactory validates and processes a CAD charge
2. PayPal processing          — PayPalProcessorFactory validates email and produces a PayPal TX ID
3. Cryptocurrency processing  — CryptoProcessorFactory validates wallet address and produces a TX hash
4. Invalid card demo          — factory catches validation failure; no payment is attempted
5. Simple Factory comparison  — same workflow via switch-based factory; shows the OCP trade-off
6. Extensibility demo         — adding a hypothetical new type requires no changes to existing factories
```

## When to Use

- You need to create objects but the exact type must be determined by a subclass or configuration, not by the calling code.
- You want to give library or framework users a hook to extend which objects get created without forking the library.
- A class cannot anticipate all the types it will need to create (plugin systems, payment gateways, notification channels).
- Each product type requires non-trivial construction logic that should not live at the call site.

## When NOT to Use

- When there is only one product type and the simplicity of `new` is sufficient.
- When the factory hierarchy would create as much complexity as the problem it is solving — a Simple Factory or a DI container may be cleaner.
- When creation logic is trivial and unlikely to change — over-engineering with a pattern here adds indirection for no gain.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Open/Closed Principle | New payment types are added by writing a new subclass — existing code is untouched |
| Single Responsibility | Each factory class is responsible for creating exactly one product type |
| Decoupled client code | Callers depend only on `PaymentProcessorFactory` and `IPaymentProcessor` — no concrete types |
| Testable | Factories can be swapped in tests; the workflow in the abstract creator is tested independently |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Class proliferation | Each product type requires a matching factory class, growing the type count quickly |
| Indirection | Two levels of abstraction (factory → processor) can obscure straightforward creation logic |
| Subclassing required | Adding a new product means a new subclass even when a lambda or delegate would suffice |

## Related Patterns

- **Abstract Factory (1.3)** — a step up: where Factory Method creates one product, Abstract Factory creates families of related products through a single interface.
- **Template Method (3.10)** — `ProcessTransaction` in the abstract creator is itself a Template Method; Factory Method and Template Method frequently appear together in this way.
- **Prototype (1.5)** — an alternative creation mechanism: instead of a factory returning a new object, Prototype returns a clone of an existing one.
- **Dependency Injection (4.05)** — in modern .NET the DI container resolves implementations by type, replacing most hand-written factories with registration (`services.AddTransient<IPaymentProcessor, CreditCardProcessor>()`).

## Running the Demo

```bash
cd src/1-Creational/1.2-FactoryMethod/FactoryMethodPattern
dotnet run
```

## Running the Tests

```bash
cd src/1-Creational/1.2-FactoryMethod/FactoryMethodPattern.Tests
dotnet test
```
