# Factory Method Pattern

## 📖 Pattern Category
**Creational Pattern**

## 🎯 Intent
Define an interface for creating an object, but let **subclasses decide which class to instantiate**. Factory Method lets a class defer instantiation to subclasses.

## 🤔 Problem
Imagine you're building a payment system that needs to support multiple payment methods — credit card, PayPal, and cryptocurrency. Each processor:
- Has different configuration data (card number vs email vs wallet address)
- Has different validation logic
- Has different processing steps

If you write a single class that handles all three, it quickly becomes a mess of `if/switch` statements. Worse, adding a fourth payment method means modifying existing code and risking regressions.

## ✅ Solution
The Factory Method pattern solves this by:
1. Defining a **common interface** (`IPaymentProcessor`) that all products implement
2. Creating an **abstract creator** (`PaymentProcessorFactory`) with the factory method (`CreateProcessor()`)
3. Having each **concrete creator** subclass override the factory method to return its own product
4. Letting client code work only with the interface — never the concrete types

## 🏗️ Structure

```
┌──────────────────────────────┐          ┌──────────────────────┐
│   PaymentProcessorFactory    │          │   IPaymentProcessor  │
│  (Abstract Creator)          │          │   (Product)          │
├──────────────────────────────┤          ├──────────────────────┤
│ # CreateProcessor() *        │─creates─▶│ + ProcessPayment()   │
│ + ProcessTransaction()       │          │ + ValidatePayment()  │
└──────────────────────────────┘          │ + GetProcessorName() │
              ▲                           └──────────────────────┘
              │                                      ▲
   ┌──────────┼──────────┐                           │
   │          │          │              ┌─────────────┼──────────────┐
   │          │          │              │             │              │
CreditCard  PayPal    Crypto      CreditCard      PayPal        Crypto
Factory     Factory   Factory     Processor       Processor     Processor
```

## 💻 Implementation in This Example

### Files:
- **IPaymentProcessor.cs** — Product interface all processors must implement
- **PaymentProcessors.cs** — Concrete products: `CreditCardProcessor`, `PayPalProcessor`, `CryptoProcessor`
- **PaymentProcessorFactory.cs** — Abstract creator + three concrete creator factories + `SimplePaymentProcessorFactory` (comparison)
- **Program.cs** — Demo showing both approaches in action

### Key Implementation Points:

**1. Product Interface** — the contract all concrete products fulfill:
```csharp
public interface IPaymentProcessor
{
    string ProcessPayment(decimal amount);
    bool ValidatePaymentMethod();
    string GetProcessorName();
}
```

**2. Abstract Creator** — defines the factory method and the standard workflow:
```csharp
public abstract class PaymentProcessorFactory
{
    protected abstract IPaymentProcessor CreateProcessor(); // factory method

    public string ProcessTransaction(decimal amount)
    {
        var processor = CreateProcessor(); // subclass decides the concrete type
        processor.ValidatePaymentMethod();
        return processor.ProcessPayment(amount);
    }
}
```

**3. Concrete Creator** — each subclass overrides only the factory method:
```csharp
public class CreditCardProcessorFactory : PaymentProcessorFactory
{
    protected override IPaymentProcessor CreateProcessor()
        => new CreditCardProcessor(_cardNumber, _cardHolderName, _expiryDate);
}
```

**4. Client code never references concrete types:**
```csharp
PaymentProcessorFactory factory = new CreditCardProcessorFactory("1234...", "John", "12/26");
factory.ProcessTransaction(99.99m); // works the same regardless of which factory
```

## 🔍 Factory Method vs Simple Factory

This example also includes `SimplePaymentProcessorFactory` to show the contrast:

| | Factory Method | Simple Factory |
|---|---|---|
| **Open/Closed** | Add new type = new subclass only | Add new type = modify the switch |
| **Flexibility** | Each factory can override more behavior | Creation logic is centralized but rigid |
| **Coupling** | Client depends only on the interface | Factory class knows all concrete types |
| **Pattern status** | GoF design pattern | Common idiom (not a formal pattern) |

Use Simple Factory when creation logic is trivial and unlikely to grow. Use Factory Method when you expect new types to be added or when different creators need different behavior.

## 🚀 How to Run

```bash
cd src/1-Creational/1.2-FactoryMethod/FactoryMethodPattern
dotnet run
```

## 🧪 Running Tests

```bash
cd src/1-Creational/1.2-FactoryMethod/FactoryMethodPattern.Tests
dotnet test
```

## 🧪 What the Demo Shows

1. **Same interface, different behavior** — all three factories call `ProcessTransaction()` identically; each produces a different processor underneath
2. **Validation per type** — each processor validates its own fields (card number format, email format, wallet address)
3. **Extensibility** — adding a new payment method (e.g., Apple Pay) requires only a new processor class + a new factory class; nothing else changes
4. **Simple Factory comparison** — the `SimplePaymentProcessorFactory` demo shows why the switch-based approach breaks down

## ✅ Benefits

| Benefit | Description |
|---------|-------------|
| **Open/Closed Principle** | Add new products by adding new subclasses — no existing code changes |
| **Single Responsibility** | Each factory is responsible for creating exactly one type of product |
| **Decoupling** | Client code depends on the interface, not on concrete implementations |
| **Encapsulation** | Construction details (which params, which class) are hidden inside the factory |

## ❌ Drawbacks

| Drawback | Description |
|----------|-------------|
| **More classes** | Each new product needs both a product class and a creator subclass |
| **Indirection** | The extra layer of abstraction can make simple cases harder to follow |
| **Inheritance required** | Forces a class hierarchy; composition-based alternatives (like delegates) can be simpler |

## 🎓 When to Use

✅ **Good Candidates:**
- When you don't know ahead of time what class you need to instantiate
- When you want subclasses to control which objects get created
- When construction logic is complex or varies per type
- Payment processors, notification senders, report generators, serializers, loggers

❌ **Bad Candidates:**
- When there's only ever one product type (no need for the abstraction)
- When construction is trivial (just `new Foo()`) — a Simple Factory is sufficient
- When you need runtime switching between types based on config (Abstract Factory or DI is better)

## 🔀 Alternatives

| Alternative | When to Use Instead |
|-------------|---------------------|
| **Simple Factory** | Construction is simple and the type list is stable |
| **Abstract Factory** | You need to create *families* of related objects together |
| **Dependency Injection** | You want a container to manage object lifetimes and wiring |
| **Strategy Pattern** | The variation is in *behavior*, not in *construction* |

## 📚 Related Patterns

- **Abstract Factory (1.3)** — uses Factory Methods internally; creates families of objects
- **Template Method (3.10)** — `ProcessTransaction()` in this example is also a Template Method
- **Singleton (1.1)** — factories are often Singletons when stateless

## 🔑 Key Takeaways

1. **The factory method is the hook** — subclasses override it to swap out the product
2. **The abstract creator still does real work** — `ProcessTransaction()` defines the standard workflow; only creation is delegated
3. **Client code stays stable** — adding a new payment method never touches existing classes
4. **Simple Factory is not the same thing** — it's a useful idiom but not the GoF Factory Method pattern

## 📖 Further Reading

- "Design Patterns: Elements of Reusable Object-Oriented Software" (Gang of Four) — Chapter 3
- "Head First Design Patterns" — Chapter 4 (excellent visual walkthrough)

---

← **Previous Pattern:** [1.1 - Singleton](../1.1-Singleton/)  
→ **Next Pattern:** [1.3 - Abstract Factory](../1.3-AbstractFactory/)
