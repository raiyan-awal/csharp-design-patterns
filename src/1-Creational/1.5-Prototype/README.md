# 1.5 — Prototype

## Intent

Create new objects by cloning an existing object (the prototype) rather than constructing them from scratch, so that spawn cost is paid once at prototype creation and then amortized across all clones.

## The Problem It Solves

Without Prototype, every spawn site must duplicate the full construction setup:

```csharp
// Without Prototype: every spawn call repeats the full config
var goblin1 = new Enemy("Goblin", "Horde", health: 50, damage: 8, armor: 2, speed: 4, xp: 10);
goblin1.Equipment.Add("Rusty Dagger");
goblin1.Equipment.Add("Leather Scraps");

var goblin2 = new Enemy("Goblin", "Horde", health: 50, damage: 8, armor: 2, speed: 4, xp: 10);
goblin2.Equipment.Add("Rusty Dagger");      // same config duplicated
goblin2.Equipment.Add("Leather Scraps");    // one typo and goblins are inconsistent

// Change base damage to 9? Hunt and update every spawn call in the codebase.
```

Problems with direct construction at every site:
- Configuration is duplicated across every call site; a single field change requires hunting every occurrence.
- List and nested-object fields are reference-shared by default — changing one clone's list changes all of them.
- Expensive setup (asset loading, network calls, complex graph initialization) is repeated per spawn.
- No single place to manage canonical base configurations for each enemy type.

## Solution: Clone-based spawning with a Registry

Implement `IPrototype<T>` on each enemy with `ShallowClone()` and `DeepClone()`, then store canonical prototypes in `EnemyRegistry` keyed by type name. Spawning becomes a single `DeepClone()` call from the registry.

```csharp
// Set up prototypes once:
var registry = new EnemyRegistry();
// registry already registers Goblin, Orc, Dragon, Boss with full equipment lists

// Spawn anywhere with one call — full deep copy, no config duplication:
var goblin = registry.Spawn("Goblin");
goblin.Health = 40;   // weaken this specific spawn — prototype is untouched
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Prototype interface | `IPrototype<T>` | Declares `ShallowClone()` and `DeepClone()` |
| Concrete prototype | `Enemy` | Implements both clone methods; has `Name`, `Faction`, `Health`, `Damage` (value types) plus `Equipment` (list) and `Stats` (reference type) |
| Nested value | `CombatStats` | Holds `Armor`, `Speed`, `XpReward`; exposes `Clone()` which returns a new independent copy |
| Registry | `EnemyRegistry` | Stores a deep clone of each registered template; `Spawn(key)` returns a fresh `DeepClone()` every time — the registry's own copy is never exposed |

## Structure

```
1.5-Prototype/
├── PrototypePattern/
│   ├── IPrototype.cs      ← generic IPrototype<T> with ShallowClone + DeepClone
│   ├── Enemy.cs           ← concrete prototype; CombatStats nested class
│   ├── EnemyRegistry.cs   ← registry of named prototypes
│   └── Program.cs
└── PrototypePattern.Tests/
    └── PrototypePatternTests.cs
```

## Key Code

### Prototype interface

```csharp
public interface IPrototype<T>
{
    T ShallowClone();   // new wrapper, shared references inside
    T DeepClone();      // new wrapper, new copies of all nested objects
}
```

Two methods are provided so the demo can contrast their behaviors side by side.

### Shallow clone — MemberwiseClone pitfall

```csharp
public Enemy ShallowClone() => (Enemy)MemberwiseClone();
```

`MemberwiseClone` copies every field by value at the bit level — safe for `int`, `string`, and other value/immutable types, but dangerous for `List<string> Equipment` and `CombatStats Stats`, which become shared references. The demo shows that `clone.Equipment.Add("sword")` also adds "sword" to the original's list.

### Deep clone — MemberwiseClone + replace reference fields

```csharp
public Enemy DeepClone()
{
    // Step 1 — copy all value-type fields cheaply (Name, Faction, Health, Damage)
    var clone = (Enemy)MemberwiseClone();

    // Step 2 — replace each shared reference with an independent copy
    clone.Equipment = new List<string>(Equipment); // new list, same immutable strings
    clone.Stats     = Stats.Clone();               // new CombatStats object
    return clone;
}
```

`CombatStats.Clone()` returns `new CombatStats { Armor = Armor, Speed = Speed, XpReward = XpReward }`. The clone is fully independent: mutating its `Stats.Armor` or `Equipment` does not affect the prototype.

### Registry — stores a clone, spawns a clone

```csharp
public sealed class EnemyRegistry
{
    private readonly Dictionary<string, Enemy> _templates =
        new(StringComparer.OrdinalIgnoreCase);

    // Register stores a deep clone — the caller's original is never held
    public void Register(string key, Enemy template)
        => _templates[key] = template.DeepClone();

    // Spawn always returns a fresh deep clone — the registry's copy is never exposed
    public Enemy Spawn(string key) =>
        _templates.TryGetValue(key, out var t)
            ? t.DeepClone()
            : throw new KeyNotFoundException($"No template registered for '{key}'");
}
```

Storing a clone on `Register` ensures external code cannot mutate the template after it has been registered, which would silently corrupt every future spawn.

## Demo Scenarios

```
1. Basic cloning             — DeepClone from a manually constructed goblin
2. Shallow copy trap         — ShallowClone shares the Equipment list; adding to the
                               clone mutates the original's equipment
3. Deep clone independence   — DeepClone produces a fully independent copy; modifying
                               health or equipment on the clone leaves the prototype unchanged
4. Prototype Registry        — EnemyRegistry.Spawn("Dragon") returns a deep clone of the
                               registered dragon prototype
5. Prototype vs new          — side-by-side comparison: direct construction vs registry spawn;
                               same result, registry is the single source of truth
```

## When to Use

- Object construction is expensive (I/O, network, complex graph) and identical or near-identical objects are needed in large numbers.
- You need many variations of a base configuration: spawn a prototype, then adjust a few fields per instance.
- The exact class of an object is unknown at the creation site but you have access to an existing instance to clone.
- You want a registry of canonical configurations that callers can clone and customize without knowing the full setup.

## When NOT to Use

- When object construction is cheap — cloning adds complexity with no performance benefit.
- When the object graph is circular or contains resources (file handles, network connections) that cannot be meaningfully cloned.
- When the number of prototype variants is small and all fields are required — a factory or constructor is simpler.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Amortized construction cost | Expensive setup runs once (prototype creation); spawns pay only clone cost |
| Single source of truth | Canonical configurations live in the registry; every spawn is consistent by default |
| Runtime configurability | Prototypes can be registered and modified at runtime without recompiling |
| Flexible customization | Clone first, then adjust a few fields — no need to know the full constructor signature |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Deep clone complexity | Nested objects, circular references, and non-cloneable resources require careful `DeepClone` implementations |
| Shallow clone bugs | Forgetting to deep-copy a nested collection is a silent correctness bug that is hard to detect |
| Clone vs constructor clarity | It is sometimes unclear whether a clone should inherit all state or only some — the semantics must be documented |

## Related Patterns

| Clone vs. reference | Shallow | Deep |
|---------------------|---------|------|
| New wrapper | Yes | Yes |
| Shared nested objects | Yes | No |
| Independent after clone | No | Yes |
| Safe to mutate clone | Only primitives/strings | Fully safe |

- **Factory Method (1.2)** — an alternative creation strategy; Factory Method constructs fresh objects, Prototype copies existing ones.
- **Singleton (1.1)** — `EnemyRegistry` is a candidate for Singleton; a single registry per application serves all spawn sites.
- **Flyweight (2.6)** — Flyweight shares immutable data between many instances; Prototype creates independent copies where callers need to mutate their own instance freely.
- **Memento (3.06)** — Memento captures and restores object state; Prototype clones it — both copy state, but Memento focuses on history / undo while Prototype focuses on spawning.

## Running the Demo

```bash
cd src/1-Creational/1.5-Prototype/PrototypePattern
dotnet run
```

## Running the Tests

```bash
cd src/1-Creational/1.5-Prototype/PrototypePattern.Tests
dotnet test
```
