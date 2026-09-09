# 1.1 — Singleton

## Intent

Ensure a class has only one instance and provide a global access point to it — useful for resources that must be shared across the entire application (configuration, logging, caching) and where multiple instances would cause inconsistency or waste.

## The Problem It Solves

Without the Singleton pattern each caller creates its own instance, so state changes made through one reference are invisible to the others:

```csharp
// Without Singleton: every 'new' produces a separate object with independent state
var config1 = new ConfigurationManager();
var config2 = new ConfigurationManager();

config1.SetSetting("Environment", "Production");
// config2.GetSetting("Environment") still returns "Development" — wrong!
```

Problems with uncontrolled instantiation:
- Multiple instances hold different copies of what should be shared state.
- Each construction pays the full creation cost every time (I/O, network, allocation).
- No single point of control to ensure the resource is initialised exactly once.
- Race conditions when two threads both call `new` at the same moment.

## Solution: Private constructor + static Lazy\<T\> instance

Make the constructor `private` to block direct instantiation, and expose the single instance through a `static` property backed by `Lazy<T>`, which handles both lazy initialization and thread-safety without explicit locks.

```csharp
// The only way to get an instance:
var config = ConfigurationManager.Instance;
config.SetSetting("Environment", "Production");

// Any other reference to Instance returns the exact same object:
var sameConfig = ConfigurationManager.Instance;
Console.WriteLine(ReferenceEquals(config, sameConfig)); // True
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Lazy Singleton | `ConfigurationManager` | Production-ready singleton; `Lazy<T>` ensures thread-safe lazy init with a private constructor |
| Eager Singleton | `EagerSingleton` | Instance created at class load time via a `static readonly` field — simpler but always pays the creation cost |
| Unsafe Singleton (anti-pattern) | `UnsafeSingleton` | Demonstrates the null-check race condition that appears without any synchronization |

## Structure

```
1.1-Singleton/
├── SingletonPattern/
│   ├── ConfigurationManager.cs   ← all three singleton variants (one file)
│   └── Program.cs
└── SingletonPattern.Tests/
    └── SingletonPatternTests.cs
```

## Key Code

### Lazy\<T\> — thread-safe initialization without locks

```csharp
public sealed class ConfigurationManager
{
    private static readonly Lazy<ConfigurationManager> _instance =
        new Lazy<ConfigurationManager>(() => new ConfigurationManager());

    private ConfigurationManager() { /* private — no external new */ }

    public static ConfigurationManager Instance => _instance.Value;
}
```

`Lazy<T>` uses `LazyThreadSafetyMode.ExecutionAndPublication` by default: only one thread runs the factory lambda, and every other concurrent caller blocks until the value is ready. The instance is created on the first call to `.Value`, not when the class is loaded.

### Eager initialization — static readonly field

```csharp
public sealed class EagerSingleton
{
    private static readonly EagerSingleton _instance = new EagerSingleton();
    private EagerSingleton() { }
    public static EagerSingleton Instance => _instance;
}
```

The CLR guarantees that static field initializers run once, in a thread-safe manner, before any code accesses the field. This is simpler than `Lazy<T>` but pays the construction cost even if the instance is never used.

### The unsafe double-check anti-pattern

```csharp
// ANTI-PATTERN — do not use in production
public static UnsafeSingleton Instance
{
    get
    {
        if (_instance == null)              // Thread A and Thread B can both pass here
            _instance = new UnsafeSingleton(); // simultaneously — two instances created
        return _instance;
    }
}
```

Two threads can both see `_instance == null` before either finishes construction, creating two separate objects. `Lazy<T>` or `static readonly` eliminates this entirely.

## Demo Scenarios

```
1. Single instance creation   — "created" message prints exactly once, no matter how many
                                times Instance is accessed
2. Shared state               — change a setting through one reference; the same change is
                                visible through every other reference
3. Thread safety              — 20 concurrent threads all access Instance; only one creation
                                event fires
4. Eager vs lazy comparison   — EagerSingleton prints at class-load time; ConfigurationManager
                                only on first .Instance access
5. UnsafeSingleton demo       — illustrates the null-check race condition
```

## When to Use

- The resource must be shared and must have exactly one instance (configuration, audit log, shared cache).
- Creation is expensive and should happen at most once (database connection pool, service locator).
- You need a well-known global access point that cannot accidentally be constructed more than once.

## When NOT to Use

- When you need different instances with different configurations — use a factory instead.
- In unit tests where isolated state per test is required — a Singleton carries state across test cases; prefer dependency injection so tests can swap implementations.
- When the "global" nature would create tight coupling between modules that should remain independent.
- When state must be reset between requests (e.g., per-request scoping in a web app).

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Controlled instantiation | Private constructor guarantees at most one instance exists |
| Thread-safe by default | `Lazy<T>` handles concurrent access without manual locking |
| Lazy initialization | Instance is created only when first needed, not at application startup |
| Global access | `ConfigurationManager.Instance` is reachable from anywhere without passing a reference |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Hard to unit test | Tests share state across runs; mocking requires dependency injection wrappers |
| Hidden dependency | Callers don't declare the dependency in their signature — it's invisible to consumers |
| SRP violation | The class both manages its own instantiation and performs its business logic |
| Global mutable state | Any code can call `SetSetting` and affect every other consumer silently |

## Related Patterns

- **Object Pool (1.6)** — often implemented as a Singleton; the pool itself has one instance managing many reusable objects.
- **Factory Method (1.2)** — can be used alongside Singleton so the factory itself is the single instance responsible for creating other objects.
- **Dependency Injection (4.05)** — the preferred alternative in testable code; DI containers enforce single-instance lifetime (`AddSingleton<T>`) without requiring the class itself to be a Singleton.
- **Service Layer (4.06)** — service-layer classes are often registered as singletons in a DI container, which achieves the same "one instance" guarantee without hard-coding it into the class.

## Running the Demo

```bash
cd src/1-Creational/1.1-Singleton/SingletonPattern
dotnet run
```

## Running the Tests

```bash
cd src/1-Creational/1.1-Singleton/SingletonPattern.Tests
dotnet test
```
