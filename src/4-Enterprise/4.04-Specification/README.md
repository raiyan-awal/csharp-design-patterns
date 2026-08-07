# 4.04 — Specification

## Intent

Encapsulate a business rule as a named, reusable, combinable object — so the rule is defined once, tested in isolation, and applied consistently whether it is filtering an in-memory collection, guarding a domain method, or generating a SQL WHERE clause.

---

## The Problem It Solves

Without Specification, the same rule gets copy-pasted as an inline lambda wherever it is needed:

```csharp
// In ProductService:
var featured = products.Where(p => p.IsActive && p.StockQuantity > 0 && p.Rating >= 4.5);

// In ReorderService:
var reorder = products.Where(p => p.IsActive && p.StockQuantity > 0 && p.StockQuantity <= 10);

// In DiscountService:
var eligible = products.Where(p => p.IsActive && p.Category == "Electronics" && p.Price > 500m);
```

Problems:
- `p.IsActive && p.StockQuantity > 0` is copy-pasted in every query — change the rule and hunt every copy
- Lambdas carry no semantic name; the reader must decode the predicate to understand intent
- Combining rules requires nesting conditions that grow hard to read
- `Repository.Find` must accept either `Func<T,bool>` (in-memory) or `Expression<Func<T,bool>>` (ORM) — a leaky abstraction

---

## Solution: Named, Composable Specification Objects

Each rule becomes a class. The class exposes two things:

| Method | Purpose |
|--------|---------|
| `IsSatisfiedBy(entity)` | Evaluate the rule on a single in-memory object |
| `ToExpression()` | Return the rule as `Expression<Func<T,bool>>` for ORM translation |

Combinators produce new specifications from existing ones:

```csharp
var featured = new ActiveSpecification()
    .And(new InStockSpecification())
    .And(new MinRatingSpecification(4.5));

// In-memory
var results = repo.Find(featured);

// EF Core — same specification, translated to SQL
var results = await _context.Products
    .Where(featured.ToExpression())
    .ToListAsync();
```

---

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| **Specification interface** | `ISpecification<T>` | Declares `IsSatisfiedBy` and `ToExpression` |
| **Abstract base** | `Specification<T>` | Compiles and caches `ToExpression`; provides `And`/`Or`/`Not` combinators |
| **Combinator** | `AndSpecification<T>` | Left && Right — combined via `Expression.AndAlso` + `Expression.Invoke` |
| **Combinator** | `OrSpecification<T>` | Left \|\| Right — combined via `Expression.OrElse` + `Expression.Invoke` |
| **Combinator** | `NotSpecification<T>` | !Inner — combined via `Expression.Not` + `Expression.Invoke` |
| **Concrete spec** | `ActiveSpecification` | `p.IsActive` |
| **Concrete spec** | `InStockSpecification` | `p.StockQuantity > 0` |
| **Concrete spec** | `CategorySpecification` | `p.Category == category` |
| **Concrete spec** | `PriceRangeSpecification` | `min <= p.Price <= max` |
| **Concrete spec** | `MinRatingSpecification` | `p.Rating >= min` |
| **Concrete spec** | `LowStockSpecification` | `0 < p.StockQuantity <= threshold` |
| **Repository** | `ProductRepository` | `Find`/`Any`/`Count` — accepts `Specification<Product>` instead of a raw predicate |

---

## Structure

```
SpecificationPattern/
│
├── Domain/
│   ├── Product.cs                  ← entity the specs evaluate
│   └── ProductRepository.cs        ← Find / Any / Count accepting Specification<T>
│
└── Specifications/
    ├── ISpecification.cs           ← IsSatisfiedBy + ToExpression
    ├── Specification.cs            ← abstract base: compiled cache + And/Or/Not
    ├── AndSpecification.cs         ← left && right (expression trees)
    ├── OrSpecification.cs          ← left || right
    ├── NotSpecification.cs         ← !inner
    ├── ActiveSpecification.cs
    ├── InStockSpecification.cs
    ├── CategorySpecification.cs
    ├── PriceRangeSpecification.cs
    ├── MinRatingSpecification.cs
    └── LowStockSpecification.cs
```

---

## Key Code

### `ISpecification<T>` — two methods, two use cases

```csharp
public interface ISpecification<T>
{
    bool                      IsSatisfiedBy(T entity);
    Expression<Func<T, bool>> ToExpression();
}
```

`IsSatisfiedBy` serves in-memory evaluation and domain guards. `ToExpression` serves ORM query translation.

### `Specification<T>` — abstract base with compiled cache and combinators

```csharp
public abstract class Specification<T> : ISpecification<T>
{
    private Func<T, bool>? _compiled;

    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity)
    {
        _compiled ??= ToExpression().Compile();   // compile once, reuse thereafter
        return _compiled(entity);
    }

    public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);
    public Specification<T> Or(Specification<T> other)  => new OrSpecification<T>(this, other);
    public Specification<T> Not()                       => new NotSpecification<T>(this);
}
```

Subclasses implement only `ToExpression()`. Compilation, caching, and all three combinators are inherited.

### `AndSpecification<T>` — combining expression trees

```csharp
public override Expression<Func<T, bool>> ToExpression()
{
    var param = Expression.Parameter(typeof(T), "x");
    var body  = Expression.AndAlso(
        Expression.Invoke(_left.ToExpression(),  param),
        Expression.Invoke(_right.ToExpression(), param));
    return Expression.Lambda<Func<T, bool>>(body, param);
}
```

Each sub-expression has its own `ParameterExpression` object. Without `Expression.Invoke`, combining two trees whose parameters are different objects causes a runtime exception ("variable 'x' referenced from scope but not defined"). `Expression.Invoke` wraps each tree as a function call that receives the single shared `param`, producing `x => left(x) && right(x)`.

### A concrete specification — one expression is all it takes

```csharp
public sealed class ActiveSpecification : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => p => p.IsActive;
}
```

Compilation, caching, And/Or/Not are all inherited. The subclass owns only the rule itself.

### `ProductRepository` — accepts a specification instead of a raw predicate

```csharp
public sealed class ProductRepository
{
    public IEnumerable<Product> Find(Specification<Product> spec)
        => _products.Where(spec.IsSatisfiedBy);

    // EF Core equivalent:
    // public Task<List<Product>> FindAsync(Specification<Product> spec)
    //     => _context.Products.Where(spec.ToExpression()).ToListAsync();
}
```

The caller passes a named rule. The repository decides whether to call `IsSatisfiedBy` or `ToExpression` based on its backing store — the caller is isolated from both.

---

## Demo Scenarios

```
PROBLEM  — inline lambdas: same rule copy-pasted in ProductService, ReorderService, DiscountService
DEMO 1   — simple specifications: Active, InStock applied individually
DEMO 2   — And: Active + InStock + Category chained
DEMO 3   — Or / Not: Electronics OR Fitness; active AND NOT Electronics
DEMO 4   — named business rules: Featured, LowStockAlert, PremiumElectronics
DEMO 5   — IsSatisfiedBy: validate a single incoming product against multiple specs
DEMO 6   — ToExpression: the same rule rendered as a SQL WHERE clause via EF Core
```

---

## When to Use

- The same business rule appears in more than one place (service, validator, repository)
- Filters need to be composed dynamically at runtime based on user input or configuration
- Domain validation and query filtering share the same rule — you want one definition, not two
- You are using an ORM and need the rule to translate to SQL rather than filter in memory

## When NOT to Use

- The filter is used exactly once in one place — a local lambda is simpler
- The query involves joins or aggregations across multiple tables — use a dedicated query object instead
- The combined specification tree would become so deep that debugging it is harder than reading an equivalent lambda

---

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Single definition** | Each rule is named and defined once; change it in one place |
| **Composable** | And / Or / Not build complex rules from simple, tested building blocks |
| **Dual-use** | `IsSatisfiedBy` for in-memory; `ToExpression` for ORM — same object, two contexts |
| **Testable in isolation** | Each concrete spec is a class — unit-test the rule without a database |
| **Communicates intent** | `featured.IsSatisfiedBy(p)` reads like a business rule, not a predicate |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **More classes** | Each rule becomes a file; a project with many rules accumulates many spec classes |
| **`Expression.Invoke` limits** | Some older or less capable LINQ providers do not support `Expression.Invoke`; parameter rebinding is the safer but more complex alternative |
| **Overhead for simple cases** | A single one-off filter does not benefit from the abstraction |

---

## Related Patterns

- **Repository (4.01)** — `FindAsync(Expression<Func<T,bool>>)` is the leaky abstraction Specification replaces; passing `spec.ToExpression()` keeps the query in the caller's vocabulary
- **CQRS (4.03)** — query handlers can accept a Specification to describe what they are looking for, keeping both sides aligned with domain vocabulary
- **Composite (2.3)** — `AndSpecification` and `OrSpecification` are composite nodes; concrete specs are leaves; the pattern is structurally a Composite restricted to boolean logic

---

## Running the Demo

```bash
cd src/4-Enterprise/4.04-Specification/SpecificationPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.04-Specification/SpecificationPattern.Tests
dotnet test
```
