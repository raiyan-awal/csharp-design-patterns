# 4.05 — Dependency Injection

## Intent

Inject a class's dependencies from outside rather than letting the class create them internally — so that each class declares what it needs through an interface, and a container wires up the concrete implementations at runtime.

---

## The Problem It Solves

Without DI, each class creates its own dependencies with `new`:

```csharp
public class CheckoutService
{
    private readonly InventoryService  _inventory = new InventoryService();
    private readonly ShoppingCart      _cart      = new ShoppingCart();
    private readonly HstCalculator     _tax       = new HstCalculator();
}
```

Problems:
- `new InventoryService()` is hard-coded — impossible to swap in a test double or alternative
- Every `CheckoutService` creates its own `InventoryService`; the expensive catalogue load repeats for every instance instead of being shared
- Lifetime is uncontrolled — nothing prevents two services from getting different `InventoryService` instances when they should share one
- Changing the tax provider means editing `CheckoutService` itself

---

## Solution: Declare Needs, Let the Container Fulfil Them

Each class declares what it needs via constructor parameters typed as interfaces. A container is configured once with concrete implementations and lifetimes; it creates and injects everything automatically.

```csharp
// 1. Declare needs — no `new`, no concrete types
public sealed class CheckoutService : ICheckoutService
{
    public CheckoutService(IShoppingCart cart, IInventoryService inventory, IHstCalculator tax)
    { ... }
}

// 2. Register once
var services = new ServiceCollection();
services.AddSingleton<IInventoryService, InventoryService>();
services.AddScoped<IShoppingCart, ShoppingCart>();
services.AddScoped<ICheckoutService, CheckoutService>();
services.AddTransient<IHstCalculator, HstCalculator>();
var provider = services.BuildServiceProvider();

// 3. Resolve — the container builds the full graph
using var scope    = provider.CreateScope();
var checkout = scope.ServiceProvider.GetRequiredService<ICheckoutService>();
```

---

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| **Service interface** | `IInventoryService` | Contract for the product catalogue |
| **Service interface** | `IShoppingCart` | Contract for a customer's cart |
| **Service interface** | `IHstCalculator` | Contract for Canadian tax calculation |
| **Service interface** | `ICheckoutService` | Contract for placing an order |
| **Singleton impl.** | `InventoryService` | Loads the product catalogue; one instance per container |
| **Scoped impl.** | `ShoppingCart` | Holds items for one session; one instance per scope |
| **Scoped impl.** | `CheckoutService` | Orchestrates cart + inventory + tax; one instance per scope |
| **Transient impl.** | `HstCalculator` | Stateless tax calculator; new instance per consumer |
| **Swap demo** | `ZeroRateCalculator` | Stub that returns 0% HST — shows implementation swapping |
| **Registration** | `ServiceRegistration` | Extension method that wires all services into `IServiceCollection` |

---

## Structure

```
DependencyInjectionPattern/
│
├── Models/
│   ├── Product.cs          ← record: Id, Name, Category, Price
│   ├── CartItem.cs         ← record: Product, Quantity, Subtotal
│   └── OrderSummary.cs     ← record: OrderId, Items, Subtotal, HstAmount, Total
│
├── Services/
│   ├── IInventoryService.cs + InventoryService.cs    ← Singleton
│   ├── IShoppingCart.cs    + ShoppingCart.cs         ← Scoped
│   ├── IHstCalculator.cs   + HstCalculator.cs        ← Transient
│   ├── ICheckoutService.cs + CheckoutService.cs      ← Scoped
│   └── ZeroRateCalculator.cs                         ← swap demo / test stub
│
└── ServiceRegistration.cs  ← AddCheckoutServices() extension method
```

---

## Key Code

### Constructor injection — dependencies declared as interfaces

```csharp
public sealed class CheckoutService : ICheckoutService
{
    private readonly IShoppingCart     _cart;
    private readonly IInventoryService _inventory;
    private readonly IHstCalculator    _tax;

    public CheckoutService(IShoppingCart cart, IInventoryService inventory, IHstCalculator tax)
    {
        _cart      = cart;
        _inventory = inventory;
        _tax       = tax;
    }
}
```

`CheckoutService` does not know what `IInventoryService` implementation it will receive — that is decided at registration time, not here.

### Service lifetimes

| Lifetime | Method | When to use |
|----------|--------|-------------|
| **Singleton** | `AddSingleton<I, T>()` | Expensive to create; safe to share; holds no per-request state |
| **Scoped** | `AddScoped<I, T>()` | Stateful per request / session; must not be shared across sessions |
| **Transient** | `AddTransient<I, T>()` | Stateless; cheap to create; no shared state needed |

```csharp
services.AddSingleton<IInventoryService, InventoryService>(); // catalogue: load once
services.AddScoped<IShoppingCart,    ShoppingCart>();         // cart: per session
services.AddScoped<ICheckoutService, CheckoutService>();      // orchestrator: per session
services.AddTransient<IHstCalculator, HstCalculator>();       // tax: stateless
```

### Scopes — creating isolation boundaries

```csharp
using var scopeA = provider.CreateScope();
var cartA = scopeA.ServiceProvider.GetRequiredService<IShoppingCart>();
// cartA is a fresh ShoppingCart for this scope

using var scopeB = provider.CreateScope();
var cartB = scopeB.ServiceProvider.GetRequiredService<IShoppingCart>();
// cartB is a different ShoppingCart — different scope, different instance
```

In ASP.NET Core, each HTTP request is automatically wrapped in a scope. In a console app you create scopes manually.

### Swapping implementations — no consumer changes

```csharp
// Production
services.AddTransient<IHstCalculator, HstCalculator>();

// Tests — swap one line, CheckoutService is unaffected
services.AddTransient<IHstCalculator, ZeroRateCalculator>();
```

`CheckoutService` receives whatever `IHstCalculator` is registered. It does not know — and does not care — which one it gets.

### Service registration as an extension method

```csharp
public static IServiceCollection AddCheckoutServices(this IServiceCollection services)
{
    services.AddSingleton<IInventoryService, InventoryService>();
    services.AddScoped<IShoppingCart, ShoppingCart>();
    services.AddScoped<ICheckoutService, CheckoutService>();
    services.AddTransient<IHstCalculator, HstCalculator>();
    return services;
}
```

Groups related registrations into a single named call. The composition root (Program.cs) stays clean and readable.

---

## Demo Scenarios

```
PROBLEM  — CheckoutService hard-codes InventoryService, ShoppingCart, HstCalculator with `new`
DEMO 1   — basic resolution: container resolves IInventoryService, lists products
DEMO 2   — Singleton: two resolutions return the same InstanceId
DEMO 3   — Scoped: same InstanceId within a scope; different InstanceId across scopes
DEMO 4   — Transient: every resolution returns a different InstanceId; HST rates by province
DEMO 5   — full checkout: cart + inventory + tax orchestrated by CheckoutService (Ontario 13% HST)
DEMO 6   — implementation swap: ZeroRateCalculator registered in place of HstCalculator;
           CheckoutService produces $0.00 HST without a single change to its own code
```

---

## When to Use

- A class has dependencies on external resources, services, or other classes
- You want to swap implementations (real vs test double, v1 vs v2) without changing consumers
- You need to control the lifetime of shared resources (one database connection per request, one cache per app)
- You are working in a team and want to enforce the Dependency Inversion Principle

## When NOT to Use

- A dependency is a simple value object or data record — passing it directly is fine
- A class is a leaf with no dependencies — DI adds no value
- The project is a small script with one or two classes — a container is overhead without benefit

---

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Testability** | Swap any dependency for a stub or mock — no consumer changes needed |
| **Lifetime control** | Singleton, Scoped, Transient enforced centrally; not scattered across the codebase |
| **Loose coupling** | Consumers depend on interfaces, not concrete types |
| **Single configuration point** | All wiring lives in `ServiceRegistration`; the rest of the app is unaware |
| **Dependency graph automation** | The container resolves transitive dependencies; you never manually build a deep graph |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **Runtime errors for missing registrations** | A forgotten `AddSingleton` becomes a runtime exception, not a compile error |
| **Captive dependency bug** | A Singleton that holds a Scoped service outlives the scope, causing stale state — hard to spot |
| **Indirection** | Following the code from a call site to the concrete implementation requires knowing what is registered |
| **Container overhead** | Adds a dependency on the DI library; small overhead for trivial apps |

---

## Related Patterns

- **Factory Method (1.2)** — the container is essentially a generic factory; `GetRequiredService<T>()` is `CreateProduct()` with the concrete type looked up from a registry
- **Service Layer (4.06)** — service classes in a service layer are typically the primary consumers of DI; the container wires repositories and services together
- **Strategy (3.09)** — DI makes it trivial to swap strategies: register a different `IHstCalculator` and every consumer automatically gets the new strategy

---

## Running the Demo

```bash
cd src/4-Enterprise/4.05-DependencyInjection/DependencyInjectionPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.05-DependencyInjection/DependencyInjectionPattern.Tests
dotnet test
```
