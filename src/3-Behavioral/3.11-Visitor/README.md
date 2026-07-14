# 3.11 — Visitor Pattern

## Intent

Represent an operation to be performed on elements of an object structure. Visitor lets you define a new operation without changing the classes of the elements on which it operates.

---

## The Problem It Solves

A shopping cart holds several product types. As the business grows, new operations are needed: tax calculation, shipping costs, loyalty points, insurance, and more. Without Visitor, every new operation must be added to every product class:

```csharp
// WITHOUT Visitor — each operation added to every element class:
public class PhysicalProduct
{
    public decimal CalculateTax()      { ... }  // new requirement
    public decimal CalculateShipping() { ... }  // new requirement
    public int     EarnLoyaltyPoints() { ... }  // new requirement
    // Every new feature = edit every product class
}

public class FoodItem
{
    public decimal CalculateTax()      { ... }  // duplicated signature
    public decimal CalculateShipping() { ... }
    public int     EarnLoyaltyPoints() { ... }
}
```

The product classes keep growing with unrelated responsibilities. Visitor fixes this by moving each operation into its own class. Product classes become stable — only their `Accept` method ever needs to be added.

---

## Solution: Shopping Cart Checkout

Four item types form a stable element structure. New checkout operations are added as visitors without touching any item class.

### Participants

| Role | Class | Responsibility |
|------|-------|---------------|
| **Element interface** | `ICartItem` | Declares `Accept(ICartVisitor)` |
| **Concrete Elements** | `PhysicalProduct`, `DigitalProduct`, `FoodItem`, `SubscriptionService` | Call `visitor.Visit(this)` in `Accept` |
| **Visitor interface** | `ICartVisitor` | One `Visit` overload per element type |
| **Concrete Visitors** | `TaxVisitor`, `ShippingVisitor`, `ReceiptVisitor`, `LoyaltyPointsVisitor` | Implement the operation for each element type |

### How each visitor applies its rules

| Visitor | Physical | Digital | Food | Subscription |
|---------|---------|---------|------|-------------|
| `TaxVisitor` | 13% HST | 13% HST | 0% (exempt) | 13% of total |
| `ShippingVisitor` | $4.99 + $1.50/kg | Free | $4.99 (+$5 refrigerated) | Free |
| `LoyaltyPointsVisitor` | 1 pt/$ | 2 pts/$ | 0 pts | 50 pts/month |
| `ReceiptVisitor` | Name + price | + `[Digital]` label | + `[Tax exempt]` label | + monthly breakdown |

---

## Structure

```
VisitorPattern/
├── ICartItem.cs           ← Element interface
├── ICartVisitor.cs        ← Visitor interface (one Visit overload per element type)
├── Items/
│   ├── PhysicalProduct.cs
│   ├── DigitalProduct.cs
│   ├── FoodItem.cs
│   └── SubscriptionService.cs
└── Visitors/
    ├── TaxVisitor.cs
    ├── ShippingVisitor.cs
    ├── ReceiptVisitor.cs
    └── LoyaltyPointsVisitor.cs
```

---

## Key Code

### Element interface — one method only

```csharp
public interface ICartItem
{
    string  Name  { get; }
    decimal Price { get; }
    void Accept(ICartVisitor visitor);
}
```

### Concrete element — Accept calls visitor.Visit(this)

```csharp
public sealed class PhysicalProduct : ICartItem
{
    public string  Name     { get; }
    public decimal Price    { get; }
    public double  WeightKg { get; }

    public void Accept(ICartVisitor visitor) => visitor.Visit(this);
}
```

Every element has the same one-line `Accept` — the type of `this` is the concrete class, so C# resolves the correct `Visit` overload at compile time.

### Visitor interface — one overload per element type

```csharp
public interface ICartVisitor
{
    void Visit(PhysicalProduct product);
    void Visit(DigitalProduct product);
    void Visit(FoodItem item);
    void Visit(SubscriptionService subscription);
}
```

### Concrete visitor — all logic for one operation in one class

```csharp
public sealed class TaxVisitor : ICartVisitor
{
    private const decimal HstRate = 0.13m;
    public decimal TotalTax { get; private set; }

    public void Visit(PhysicalProduct p) { TotalTax += Math.Round(p.Price * HstRate, 2); ... }
    public void Visit(DigitalProduct p)  { TotalTax += Math.Round(p.Price * HstRate, 2); ... }
    public void Visit(FoodItem f)        { /* zero — basic groceries exempt */ }
    public void Visit(SubscriptionService s) { TotalTax += Math.Round(s.Price * HstRate, 2); ... }
}
```

### Usage

```csharp
IReadOnlyList<ICartItem> cart = [ new PhysicalProduct(...), new DigitalProduct(...), ... ];

var tax      = new TaxVisitor();
var shipping = new ShippingVisitor();

foreach (var item in cart)
{
    item.Accept(tax);
    item.Accept(shipping);
}

Console.WriteLine($"Tax: ${tax.TotalTax:F2}  Shipping: ${shipping.TotalShipping:F2}");
```

---

## Double Dispatch

Visitor relies on **double dispatch** — the method called depends on the runtime type of *both* the element and the visitor:

```
item.Accept(visitor)
  └─ dispatches on item's type  → PhysicalProduct.Accept
       └─ visitor.Visit(this)
            └─ dispatches on visitor's type → TaxVisitor.Visit(PhysicalProduct)
```

C# normally only dispatches on the receiver (`item`). The two-step `Accept` → `Visit(this)` achieves a second dispatch on `this`'s concrete type. Without this pattern, you'd need a `switch` on type or `is` checks inside the visitor.

---

## Demo Scenarios

```
DEMO 1 — Receipt: formatted line per item with type-specific labels
DEMO 2 — Tax: 13% HST on taxable items; food is exempt
DEMO 3 — Shipping: weight-based for physical; free for digital; refrigerated surcharge for food
DEMO 4 — Full checkout summary: subtotal + tax + shipping = order total
DEMO 5 — New visitor (LoyaltyPoints) added with zero changes to any item class
```

---

## When to Use

- You need to perform many distinct operations on a stable set of types
- Adding operations by modifying element classes would bloat them with unrelated concerns
- The element class hierarchy rarely changes (adding a new element type means updating all visitors)

---

## When NOT to Use

- The element hierarchy changes frequently — every new type requires editing all visitors
- Only one or two operations exist — the overhead of the pattern isn't justified
- Elements need to encapsulate their behaviour tightly (Visitor breaks encapsulation by exposing element internals to visitors)

---

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Open/Closed for operations** | New operations = new visitor class, no element changes |
| **Single Responsibility** | Each visitor contains exactly one operation across all types |
| **Accumulation** | Visitors naturally accumulate results (TotalTax, TotalShipping, Lines) |
| **Multiple visitors per traversal** | Run tax, shipping, and receipt in one loop pass |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **Closed for new element types** | Adding `GiftCard` requires editing every existing visitor |
| **Breaks encapsulation** | Visitors access element properties directly — elements can't hide their internals |
| **Visitor interface bloat** | One `Visit` overload per element — grows with the hierarchy |
| **Double dispatch complexity** | The `Accept`/`Visit` indirection is non-obvious to new readers |

---

## Visitor vs Iterator

| | Visitor | Iterator |
|---|---------|---------|
| **Purpose** | Apply an operation to each element | Traverse elements without exposing structure |
| **Who provides logic** | The visitor | The caller (receives elements one by one) |
| **Result** | Visitor accumulates it | Caller accumulates it |
| **New operation** | New visitor class | New loop body — no new class |

---

## Related Patterns

- **Composite (2.3)** — Visitor commonly traverses a Composite tree (`Accept` recurses into children)
- **Iterator (3.04)** — often used together: Iterator provides the traversal, Visitor provides the operation
- **Command (3.02)** — a visitor can be wrapped as a Command to queue or undo a whole-cart operation
- **Strategy (3.09)** — similar intent (encapsulate an operation) but Strategy swaps one algorithm; Visitor applies one operation across many types

---

## Running the Demo

```bash
cd src/3-Behavioral/3.11-Visitor/VisitorPattern
dotnet run
```

## Running the Tests

```bash
cd src/3-Behavioral/3.11-Visitor/VisitorPattern.Tests
dotnet test
```
