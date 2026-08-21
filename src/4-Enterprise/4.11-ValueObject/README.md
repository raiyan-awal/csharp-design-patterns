# 4.11 — Value Object

## Intent

A Value Object is an object whose identity is determined entirely by its attributes, not by a unique identifier. Two Value Objects with the same attribute values are considered equal and interchangeable. They are always immutable — any operation that would change the value returns a new Value Object instead of mutating the original.

## The Problem It Solves

```csharp
// Without Value Objects: scalars scattered through the domain
public class PropertyListing
{
    public decimal Price { get; set; }        // CAD? USD? What currency?
    public string  PostalCode { get; set; }   // "M5H2N2"? "m5h 2n2"? Both valid?
    public DateTime AvailableFrom { get; set; }
    public DateTime AvailableTo { get; set; } // nothing prevents AvailableTo < AvailableFrom

    public bool IsAvailableOn(DateTime date) => date >= AvailableFrom && date <= AvailableTo;
}

// Equality is identity — two "equal" prices are actually different objects
var a = new { Price = 750_000m, Currency = "CAD" };
var b = new { Price = 750_000m, Currency = "CAD" };
Console.WriteLine(a == b); // comparing references — meaningless for values
```

Problems:

- **Primitive obsession** — a `decimal` price has no currency, no validation, no operations like `Add` or `MultiplyBy`.
- **Missing invariants** — nothing stops `AvailableTo < AvailableFrom`, `PostalCode = "INVALID"`, or `Price = -50`.
- **Shotgun equality** — to compare two "prices," callers must check both `decimal` and `string` fields manually every time.
- **Duplication** — normalization (postal code casing) and range validation repeat across every class that uses these raw types.

## Solution: Encapsulate Value and Behaviour in an Immutable Object

```csharp
// Structural equality works out of the box with record types
var price1 = new Money(750_000m, "CAD");
var price2 = new Money(750_000m, "cad");  // currency normalized to "CAD"
Console.WriteLine(price1 == price2);      // true — same value, regardless of object identity

// Immutable arithmetic — original unchanged
var hst   = price1 * 0.13m;
var total = price1 + hst;

// Invariants enforced at construction, not scattered in callers
var range = new DateRange(start, end);    // throws if end < start — once, in one place
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Value Object | `Money` | CAD monetary amount; structural equality; `+`, `-`, `*` operators; currency normalization |
| Value Object | `Address` | Canadian street address; postal code normalization to `A1A 1A1` format; structural equality |
| Value Object | `DateRange` | Inclusive date interval; `Contains`, `Overlaps`, `Intersection`; validates `End >= Start` |
| Domain Entity | `PropertyListing` | Entity that composes all three value objects; `WithPrice` / `WithAvailability` return new instances |

## Structure

```
4.11-ValueObject/
├── ValueObjectPattern/
│   ├── Values/
│   │   ├── Money.cs          ← readonly record struct; amount + currency; arithmetic operators
│   │   ├── Address.cs        ← record class; postal code normalized in constructor
│   │   └── DateRange.cs      ← readonly record struct; Contains, Overlaps, Intersection
│   ├── Domain/
│   │   └── PropertyListing.cs ← entity that embeds all three value objects
│   └── Program.cs            ← 4-section demo: Money, Address, DateRange, PropertyListing
└── ValueObjectPattern.Tests/
    └── ValueObjectTests.cs   ← 33 tests: equality, immutability, validation, operations
```

## Key Code

### `readonly record struct` — structural equality for free

```csharp
public readonly record struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));
        Amount   = amount;
        Currency = currency.ToUpperInvariant();
    }
}

var a = new Money(750_000m, "CAD");
var b = new Money(750_000m, "cad");
Console.WriteLine(a == b); // true — the record generates Equals/GetHashCode from properties
```

C# `record` types (both `record class` and `record struct`) auto-generate `Equals`, `GetHashCode`, and `==`/`!=` based on all declared properties. This replaces the tedious boilerplate of manual value equality. `readonly record struct` adds the constraint that no property can be reassigned after construction, enforcing immutability at the compiler level.

### Immutable operations return new values

```csharp
public Money Add(Money other)
{
    EnsureSameCurrency(other);
    return new(Amount + other.Amount, Currency); // new instance — original untouched
}

public static Money operator +(Money a, Money b) => a.Add(b);
```

Every arithmetic method returns a new `Money`. Callers can safely pass a `Money` to any method without worrying that it will be modified, because it structurally cannot be.

### Normalization in the constructor — single point of truth

```csharp
public Address(string street, string city, string province, string postalCode)
{
    Street     = street;
    City       = city;
    Province   = province;
    PostalCode = NormalizePostalCode(postalCode); // "m5h2n2" → "M5H 2N2" every time
}

private static string NormalizePostalCode(string raw)
{
    var m = PostalPattern.Match(raw.Trim());
    if (!m.Success) throw new ArgumentException($"'{raw}' is not a valid Canadian postal code.");
    return $"{m.Groups[1].Value.ToUpperInvariant()} {m.Groups[2].Value.ToUpperInvariant()}";
}
```

Any `Address` constructed with `"m5h2n2"`, `"M5H2N2"`, or `"m5h 2n2"` will store `"M5H 2N2"` — and therefore compare equal. No caller ever writes normalization logic.

### `DateRange` behavioural operations

```csharp
public bool Contains(DateOnly date)    => date >= Start && date <= End;
public bool Overlaps(DateRange other)  => Start <= other.End && End >= other.Start;
public DateRange? Intersection(DateRange other)
{
    var start = Start > other.Start ? Start : other.Start;
    var end   = End   < other.End   ? End   : other.End;
    return start <= end ? new DateRange(start, end) : null;
}
```

These operations belong on `DateRange` — not on a `PropertyService` or a utility class — because they are calculations entirely about the values of Start and End. Placing them here means every caller gets them for free, and the logic lives in one place.

## Demo Scenarios

```
=== Maple Properties — Value Object Demo ===

--- Money: Structural Equality ---
price1 == price2 (same amount, different case): true
price1 == price3 (different amount): false

--- Money: Immutable Arithmetic ---
Asking: $875,000.00 CAD  →  HST 13%: $113,750.00  →  Total: $988,750.00
askingPrice unchanged: $875,000.00 CAD

--- Address: Structural Equality & Normalization ---
addr1 == addr2 (one created with 'm5h 2n2'): true
addr1 == addr3 (different city): false

--- DateRange: Operations ---
Target date in listed range: true
Viewing overlaps listed: true
OffSeason overlaps listed: false
Intersection: 2026-10-01 to 2026-11-30 (61 days)

--- PropertyListing: Composing Value Objects ---
Reduce price → new listing; original asking price unchanged.
Two listings at same address: Location equal, Availability equal.
```

## When to Use

- A concept in your domain is defined by its attributes, not a database identity (money, coordinates, date ranges, email addresses, phone numbers, colour values, measurements).
- You want to eliminate primitive obsession — replace `decimal price, string currency` pairs with a single meaningful type that has behaviour.
- You need to enforce invariants (non-negative amount, valid postal code, end-after-start) in one place rather than across all callers.
- You want structural equality without writing `Equals`/`GetHashCode` by hand.

## When NOT to Use

- The concept has a meaningful lifecycle and must be tracked by identity across time (use an Entity instead).
- The object must be mutated in place for performance (e.g., high-frequency simulation — consider `struct` with no immutability guarantee, or mutable classes).
- The value object would be so large that copying it on every operation is prohibitively expensive.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Structural equality | Two value objects with the same attributes are interchangeable — no reference comparison bugs. |
| Invariant enforcement | Validation lives in the constructor; invalid states cannot be constructed. |
| Normalization | Canonical forms (postal code casing, currency code casing) are applied once at construction, never repeated. |
| Behaviour encapsulation | Operations like `Overlaps`, `Add`, and `MultiplyBy` live on the type, not in service classes. |
| Safe sharing | Immutability means value objects can be shared freely across the object graph without defensive copying. |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Copying overhead | Every operation produces a new object; for hot paths with many small allocations this can pressure the GC (use `readonly struct` to stack-allocate). |
| Persistence mapping | ORMs need configuration to map a value object to columns rather than a separate table; EF Core uses `OwnsOne` for this. |
| Serialization | JSON serializers may not reconstruct via the constructor — may need a custom converter or `[JsonConstructor]`. |

## Related Patterns

- **Entity (4.14)** — the counterpart; entities have identity across time, value objects do not. A `PropertyListing` is an entity; its `Money`, `Address`, and `DateRange` are value objects.
- **Aggregate Root (4.13)** — value objects are commonly owned by aggregate roots and entities within the aggregate; they cannot exist independently and share their owner's lifecycle.
- **Domain Event (4.12)** — domain events frequently carry value objects as payloads (e.g., `PriceChangedEvent` holds a `Money` oldPrice and a `Money` newPrice).
- **Specification (4.04)** — specifications can be written against value objects; a `PriceRange` spec may accept `Money` boundaries and evaluate candidates using value equality.

## Running the Demo

```bash
cd src/4-Enterprise/4.11-ValueObject/ValueObjectPattern && dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.11-ValueObject/ValueObjectPattern.Tests && dotnet test
```
