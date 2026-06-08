# 2.6 Flyweight Pattern

## Intent

Use sharing to support large numbers of fine-grained objects efficiently. Extract the state that can be shared (intrinsic) from the state that varies per instance (extrinsic), and store intrinsic state only once.

---

## The Problem It Solves

You need to render 100,000 trees on a map. Each tree has a species name, colour, and texture — but also a unique position and size. The naïve approach gives every tree its own copy of the type data:

```csharp
// Without Flyweight — 100,000 trees × 3 duplicated strings each
class Tree
{
    int    X, Y, HeightCm, AgeYears;   // unique per tree    ← fine
    string Species = "Oak";             // same for all Oaks  ← wasted
    string Colour  = "dark green";      // same for all Oaks  ← wasted
    string Texture = "oak_bark.png";    // same for all Oaks  ← wasted
}
```

With Flyweight, the shared data lives in one object. Every Oak tree holds only a reference to it:

```csharp
// With Flyweight — one TreeType object per species, shared by all trees
class Tree
{
    int      X, Y, HeightCm, AgeYears;  // unique per tree  (24 bytes)
    TreeType _type;                      // shared reference  (8 bytes)
}

// TreeType (flyweight) — created once, reused by all Oaks:
class TreeType { string Species; string Colour; string Texture; }
```

100,000 trees, 5 species → 5 `TreeType` objects instead of 100,000.

---

## The Two Kinds of State

| State | Where stored | Who owns it |
|-------|-------------|-------------|
| **Intrinsic** | `TreeType` flyweight | Shared — one object per unique key |
| **Extrinsic** | `Tree` context | Unique per instance — passed to flyweight at runtime |

The flyweight method receives extrinsic state as parameters — it never stores it:

```csharp
// TreeType.Draw() receives extrinsic state each time — never stores it
public void Draw(int x, int y, int heightCm, int ageYears)
    => Console.WriteLine($"[{Species}] at ({x},{y}) h={heightCm}cm ...");

// Tree delegates, passing its own extrinsic fields:
public void Draw() => _type.Draw(X, Y, HeightCm, AgeYears);
```

---

## Domain: Forest Simulation

| Role | Class | Description |
|------|-------|-------------|
| **Flyweight** | `TreeType` | Intrinsic: species, colour, texture — immutable, shared |
| **Context** | `Tree` | Extrinsic: X, Y, height, age — unique per tree |
| **Factory** | `TreeTypeFactory` | Cache: returns existing `TreeType` or creates one |
| **Client** | `Forest` | Plants trees; uses factory transparently |

---

## Structure

```
Forest
  │
  ├── Tree (x=10, y=20, h=500)  ──▶ TreeType("Oak", "dark green", "oak.png")  ◀──┐
  ├── Tree (x=15, y=45, h=750)  ──▶  (same object)                               │  shared
  ├── Tree (x=80, y=10, h=620)  ──▶  (same object)                            ───┘
  │
  ├── Tree (x=30, y=60, h=300)  ──▶ TreeType("Pine", "bright green", "pine.png") ◀──┐
  └── Tree (x=55, y=75, h=420)  ──▶  (same object)                               ───┘ shared
```

`ReferenceEquals(oaks[0].Type, oaks[1].Type)` returns `true` — they point to the same object.

---

## The Factory: Making Sharing Transparent

The factory is what makes the pattern work invisibly. The client just calls `PlantTree()` with all the data it has:

```csharp
forest.PlantTree(10, 20, 500, 30, "Oak", "dark green", "oak.png");
forest.PlantTree(15, 45, 750, 50, "Oak", "dark green", "oak.png");
// Both calls produce trees that share the same TreeType — client does not manage this
```

Inside the factory:

```csharp
public TreeType GetOrCreate(string species, string colour, string texture)
{
    string key = $"{species}|{colour}|{texture}";
    if (!_cache.TryGetValue(key, out var type))
    {
        type = new TreeType(species, colour, texture);
        _cache[key] = type;
    }
    return type;   // returns cached instance on repeat calls
}
```

---

## Memory Savings (Approximate)

For 100,000 trees across 5 species:

| | Without Flyweight | With Flyweight |
|---|---|---|
| Type data | 100,000 × ~222 bytes | 5 × ~222 bytes |
| Context data | 100,000 × 24 bytes | 100,000 × 24 bytes |
| **Total** | **~24.4 MB** | **~2.5 MB** |
| **Saving** | — | **~90%** |

The saving scales with the number of objects and how much intrinsic state they share.

---

## When to Use

- You need a large number of similar objects (thousands to millions)
- Most of each object's state can be computed from a small shared subset
- The memory cost of all those objects is causing real problems

## When NOT to Use

- The number of objects is small — the factory overhead isn't worth it
- Most of an object's state is unique — there's nothing to share
- The complexity of splitting intrinsic/extrinsic state outweighs the memory gain

---

## Benefits

- Dramatic reduction in memory when many objects share state
- The factory keeps sharing transparent — callers are unaware of it

## Drawbacks

- Splits natural object state into two places — harder to reason about
- Extrinsic state must be recomputed or passed around on every operation
- The factory adds a layer of indirection and complexity

---

## Running the Demo

```bash
cd src/2-Structural/2.6-Flyweight/FlyweightPattern
dotnet run
```

## Running Tests

```bash
cd src/2-Structural/2.6-Flyweight/FlyweightPattern.Tests
dotnet test
```

---

## Related Patterns

- **Singleton** — the factory cache gives each key a singleton-like instance; Singleton is one global instance, Flyweight is one instance per key
- **Composite** — Flyweight is often applied to the leaf nodes of a Composite tree (e.g. characters in a text document)
- **Strategy** — can be implemented as a Flyweight if the strategy objects are stateless and safely shareable

---

### Flyweight vs Singleton

```csharp
// Singleton — exactly ONE instance for the whole application:
public sealed class AppConfig
{
    public static AppConfig Instance { get; } = new();
    private AppConfig() { }
}

// Flyweight — ONE instance PER KEY (not per application):
var oak  = factory.GetOrCreate("Oak",  "green", "oak.png");   // key 1
var pine = factory.GetOrCreate("Pine", "green", "pine.png");  // key 2
// oak and pine are different objects; two calls with "Oak" return the same object
```

Singleton enforces a single instance globally. Flyweight enforces a single instance per unique combination of intrinsic state — you can have many flyweight instances, just not duplicates.
