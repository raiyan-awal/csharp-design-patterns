# 3.09 — Strategy Pattern

## Intent

Define a family of algorithms, encapsulate each one, and make them interchangeable. Strategy lets the algorithm vary independently from the clients that use it.

---

## The Problem It Solves

A navigation app needs to plan routes differently depending on how the user wants to travel. Without Strategy, the context ends up with a growing chain of conditionals:

```csharp
// WITHOUT Strategy — Navigator owns all algorithms:
public Route GetRoute(Location origin, Location destination, string mode)
{
    if (mode == "driving")
    {
        // ... highway detour logic, fuel cost calculation ...
    }
    else if (mode == "walking")
    {
        // ... direct path logic ...
    }
    else if (mode == "cycling")
    {
        // ... bike-trail logic ...
    }
    else if (mode == "transit")
    {
        // ... stop/transfer logic, flat fare ...
    }
    // Add a new mode? Edit Navigator.
}
```

Every new travel mode means editing the `Navigator` class. Strategy fixes this by moving each algorithm into its own class — `Navigator` stays unchanged when modes are added or removed.

---

## Solution: Route Planner

Four strategies calculate routes for different travel modes. Each encapsulates its own path multiplier, speed, and cost model. The `Navigator` context delegates everything and never branches on mode.

### Participants

| Role | Class | Responsibility |
|------|-------|---------------|
| **Strategy interface** | `IRouteStrategy` | `Name`, `Calculate(origin, destination)` |
| **Concrete Strategies** | `DrivingStrategy`, `WalkingStrategy`, `CyclingStrategy`, `PublicTransitStrategy` | Encapsulate one routing algorithm each |
| **Context** | `Navigator` | Holds the active strategy; delegates `GetRoute` to it |

### How each strategy models travel

| Strategy | Path multiplier | Avg speed | Cost model |
|----------|:-:|:-:|--------|
| `DrivingStrategy` | 1.25× (highway detour) | 70 km/h | $0.18 / km |
| `CyclingStrategy` | 1.10× (trail detour) | 18 km/h | Free |
| `PublicTransitStrategy` | 1.40× (route detour) | 28 km/h | $3.30 flat fare |
| `WalkingStrategy` | 1.05× (near-direct) | 5 km/h | Free |

All strategies share the same `GeoHelper.HaversineKm` baseline distance; the multiplier represents real-world detour overhead for that mode.

---

## Structure

```
StrategyPattern/
├── IRouteStrategy.cs     ← Strategy interface
├── Location.cs           ← record: Name, Lat, Lng
├── Route.cs              ← record: Mode, DistanceKm, Duration, Cost, Steps
├── GeoHelper.cs          ← Haversine distance calculation (internal utility)
├── Navigator.cs          ← Context: holds and delegates to IRouteStrategy
└── Strategies/
    ├── DrivingStrategy.cs
    ├── WalkingStrategy.cs
    ├── CyclingStrategy.cs
    └── PublicTransitStrategy.cs
```

---

## Key Code

### Strategy interface — one method, clearly defined contract

```csharp
public interface IRouteStrategy
{
    string Name { get; }
    Route Calculate(Location origin, Location destination);
}
```

### Context — delegates entirely, no branching

```csharp
public sealed class Navigator
{
    private IRouteStrategy _strategy;

    public Navigator(IRouteStrategy strategy) => _strategy = strategy;

    public void SetStrategy(IRouteStrategy strategy) => _strategy = strategy;

    public Route GetRoute(Location origin, Location destination)
        => _strategy.Calculate(origin, destination);  // no if/switch here
}
```

### Concrete strategy — all algorithm details in one place

```csharp
public sealed class DrivingStrategy : IRouteStrategy
{
    private readonly double  _pathMultiplier;
    private readonly double  _avgSpeedKmh;
    private readonly decimal _costPerKm;

    public DrivingStrategy(
        double  pathMultiplier = 1.25,
        double  avgSpeedKmh   = 70.0,
        decimal costPerKm     = 0.18m)
    { ... }

    public string Name => "Driving";

    public Route Calculate(Location origin, Location destination)
    {
        var straight = GeoHelper.HaversineKm(origin, destination);
        var distance = straight * _pathMultiplier;
        var hours    = distance / _avgSpeedKmh;
        var cost     = (decimal)distance * _costPerKm;

        return new Route(
            Mode:       Name,
            DistanceKm: Math.Round(distance, 1),
            Duration:   TimeSpan.FromHours(hours),
            Cost:       Math.Round(cost, 2),
            Steps:      [ ... ]);
    }
}
```

### Transit uses a flat fare regardless of distance

```csharp
public Route Calculate(Location origin, Location destination)
{
    var distance = GeoHelper.HaversineKm(origin, destination) * _pathMultiplier;
    var hours    = distance / _avgSpeedKmh;

    return new Route(
        Mode: Name,
        ...,
        Cost: _flatFare,  // same price whether you go 2 km or 20 km
        ...);
}
```

### Usage

```csharp
var nav = new Navigator(new DrivingStrategy());
nav.GetRoute(unionStation, pearsonAirport);     // highway route, $4.17

nav.SetStrategy(new PublicTransitStrategy());
nav.GetRoute(unionStation, pearsonAirport);     // TTC route, $3.30 flat

nav.SetStrategy(new WalkingStrategy());
nav.GetRoute(unionStation, pearsonAirport);     // direct walk, Free
```

---

## Demo Scenarios

```
DEMO 1 — Same route (Union Station → Pearson), all four strategies compared
DEMO 2 — Runtime swap: Drive home Friday, Transit Saturday, Cycle Sunday
DEMO 3 — Short trip: walking vs driving (detour overhead vs convenience)
DEMO 4 — Long trip summary table: distance, duration, and cost side by side
```

---

## When to Use

- Multiple variants of an algorithm exist and should be swappable at runtime
- A class has a large conditional that selects between behaviours
- You want to isolate algorithm logic so it can be tested and changed independently

---

## When NOT to Use

- Only one algorithm ever — abstraction adds cost with no benefit
- Algorithms differ by a single parameter — use a configuration value instead
- The client never needs to switch — a direct method call is simpler

---

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Open/Closed** | Add new strategies without modifying the context |
| **Single Responsibility** | Each algorithm lives in its own class |
| **Testable in isolation** | Each strategy can be unit tested independently |
| **Runtime flexibility** | Strategy can be swapped while the program is running |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **Client must know strategies** | The client code decides which strategy to use — it must know they exist |
| **Class proliferation** | One class per algorithm variant |
| **Overkill for simple cases** | If there are only two options, a boolean flag is often enough |

---

## Strategy vs State

Both delegate behaviour through an interface, but they solve different problems:

| | Strategy | State |
|---|---------|-------|
| **Who switches** | Client explicitly calls `SetStrategy` | States trigger their own transitions |
| **Coupling** | Strategies are independent of each other | Concrete states often reference each other |
| **Purpose** | Select one algorithm from a family | Alter behaviour as an object's internal state changes |
| **Example** | Route calculation mode | Vending machine lifecycle (Idle → HasMoney → Dispensing) |

---

## Related Patterns

- **State (3.08)** — similar structure; differs in who drives the change and why
- **Template Method (3.10)** — defines the algorithm skeleton in a base class; Strategy replaces the whole algorithm
- **Command (3.02)** — a Command can wrap a Strategy to queue or undo algorithm choices
- **Factory Method (1.02)** — can be used to create the right Strategy without the client knowing the concrete type

---

## Running the Demo

```bash
cd src/3-Behavioral/3.09-Strategy/StrategyPattern
dotnet run
```

## Running the Tests

```bash
cd src/3-Behavioral/3.09-Strategy/StrategyPattern.Tests
dotnet test
```
