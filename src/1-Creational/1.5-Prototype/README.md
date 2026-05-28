# Prototype Pattern

## 📖 Pattern Category
**Creational Pattern**

## 🎯 Intent
Specify the kinds of objects to create using a **prototypical instance**, and create new objects by **cloning** that prototype rather than constructing from scratch.

## 🤔 Problem
A game level needs to spawn 1,000 goblins. Each goblin has the same base stats, the same starting equipment, and the same faction — but after spawning they diverge (one gets a sword, another loses health, etc.).

With `new`, you repeat the full construction config in every spawn call. Change the base damage? You hunt down every call site. And construction may be expensive if it involves database lookups, deep object graphs, or heavy computation.

## ✅ Solution
Define one **template** (the prototype) with the desired starting state. New instances are created by **cloning** the template:

1. The prototype implements a `Clone()` method (or two: `ShallowClone` and `DeepClone`)
2. Cloning copies the template's state into the new object in one cheap operation
3. The new object is then customised if needed — the template is never touched
4. A **Registry** stores named templates and hands out fresh clones on demand

## 🏗️ Structure

```
    «interface»
    IPrototype<T>
┌────────────────────┐
│ ShallowClone() → T │
│ DeepClone()    → T │
└────────────────────┘
         ▲
         │ implements
      Enemy                           EnemyRegistry
┌─────────────────────┐          ┌──────────────────────────────┐
│ Name: string        │          │ - _templates: Dict<string,   │
│ Faction: string     │◄─────────│              Enemy>          │
│ Health: int         │          │ Register(key, Enemy)         │
│ Damage: int         │          │ Spawn(key) → Enemy           │
│ Equipment: List<T>  │          │ IsRegistered(key): bool      │
│ Stats: CombatStats  │          └──────────────────────────────┘
│                     │               stores deep clones,
│ ShallowClone()      │               returns deep clones
│ DeepClone()         │
└─────────────────────┘
         │
         │ contains
         ▼
    CombatStats
┌─────────────────┐
│ Armor: int      │
│ Speed: int      │
│ XpReward: int   │
│ Clone()         │
└─────────────────┘
```

## 💻 Implementation in This Example

### Files:
- **IPrototype.cs** — Generic prototype interface with explicit `ShallowClone()` and `DeepClone()`
- **Enemy.cs** — Concrete prototype (`Enemy`) + nested value object (`CombatStats`)
- **EnemyRegistry.cs** — Prototype registry; stores and spawns named enemy templates
- **Program.cs** — Demo with 5 demonstrations + Pause() between each

### Key Implementation Points:

**1. Why a custom interface instead of `ICloneable`?**

`ICloneable` has two well-known problems:
- Returns `object` — every caller must cast
- Doesn't say whether the clone is shallow or deep — the contract is ambiguous

```csharp
// Our explicit interface — no ambiguity, no casting
public interface IPrototype<T>
{
    T ShallowClone();
    T DeepClone();
}
```

**2. ShallowClone via `MemberwiseClone()`**

```csharp
public Enemy ShallowClone() => (Enemy)MemberwiseClone();
// Copies value types → safe
// Copies reference addresses → List<string> and CombatStats are SHARED
```

**3. DeepClone — replace every reference-type field**

```csharp
public Enemy DeepClone()
{
    var clone = (Enemy)MemberwiseClone();    // Step 1: copy value types cheaply
    clone.Equipment = new List<string>(Equipment);  // Step 2: new list
    clone.Stats     = Stats.Clone();                // Step 2: new CombatStats
    return clone;
}
```

**4. Registry — one template, many independent spawns**

```csharp
public sealed class EnemyRegistry
{
    private readonly Dictionary<string, Enemy> _templates = new();

    public void Register(string key, Enemy template)
        => _templates[key] = template.DeepClone();  // stores a clone, not the original

    public Enemy Spawn(string key)
        => _templates[key].DeepClone();             // every spawn is independent
}
```

## 🔍 Shallow Copy vs Deep Copy

This is the most important concept in the Prototype pattern:

| | Shallow Clone | Deep Clone |
|---|---|---|
| **Value types** (`int`, `bool`, `struct`) | ✅ Independent copy | ✅ Independent copy |
| **Immutable refs** (`string`) | ✅ Safe — mutations create new string | ✅ Safe |
| **Mutable refs** (`List<T>`, custom class) | ❌ Shared — mutation affects original | ✅ New object — fully independent |
| **Speed** | Fast (one `MemberwiseClone` call) | Slower (allocates new nested objects) |
| **Use when** | Nested objects are read-only or you want sharing | Nested objects are mutable and must be independent |

## 🚀 How to Run

```bash
cd src/1-Creational/1.5-Prototype/PrototypePattern
dotnet run
```

## 🧪 Running Tests

```bash
cd src/1-Creational/1.5-Prototype/PrototypePattern.Tests
dotnet test
```

## 🧪 What the Demo Shows

1. **Basic cloning** — clone a template, modify each spawn independently, template stays unchanged
2. **The shallow copy trap** — mutating a shallow clone's equipment also mutates the original (shared list)
3. **Deep clone** — fully independent copies; `ReferenceEquals` confirms separate objects
4. **Prototype Registry** — register named templates, spawn waves of enemies, customise spawns without corrupting the registry
5. **Prototype vs `new`** — when cloning beats constructing from scratch

## ✅ Benefits

| Benefit | Description |
|---------|-------------|
| **Avoids expensive construction** | Clone is cheaper than re-running complex initialisation logic |
| **Single source of truth** | Change the template → all future spawns inherit the change |
| **Unknown concrete types** | You can clone an object without knowing its exact class (polymorphic clone) |
| **Fine-grained control** | ShallowClone for cheap copies, DeepClone when independence is required |

## ❌ Drawbacks

| Drawback | Description |
|----------|-------------|
| **Cloning complex graphs is hard** | Circular references, lazy-loaded properties, and external resources make DeepClone tricky |
| **Hidden coupling** | Callers don't see the construction details — bugs in the template silently propagate to all clones |
| **`MemberwiseClone` is protected** | You must implement the clone method inside the class — no external cloning |

## 🎓 When to Use

✅ **Good Candidates:**
- Spawning many similar objects (game entities, report instances, test fixtures)
- Construction involves expensive operations (DB queries, network calls, heavy computation)
- You need to copy an object without knowing its concrete type at compile time
- You want a "default configuration" that can be tweaked per-instance

❌ **Bad Candidates:**
- Objects with circular references (require careful cycle detection in DeepClone)
- Objects that hold unmanaged resources (file handles, sockets) — cloning them is dangerous
- Simple objects where `new` with named parameters is clearer

## 🔀 Alternatives

| Alternative | When to Use Instead |
|-------------|---------------------|
| **Builder (1.4)** | Object has many optional fields and you want named, step-by-step construction |
| **Factory Method (1.2)** | You need to vary the *type* of object, not just its starting state |
| **Abstract Factory (1.3)** | You need families of related objects, not copies of one template |
| **JSON round-trip** | Quick deep clone when the object is JSON-serialisable: `JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj))` |

## 📚 Related Patterns

- **Builder (1.4)** — Prototype starts from an existing instance; Builder assembles from nothing
- **Abstract Factory (1.3)** — Abstract Factory can use Prototype to store and return product prototypes
- **Composite (2.3)** — Prototypes are often used to clone Composite trees
- **Memento (3.06)** — Both capture state; Memento stores it for undo, Prototype copies it for new instances

## 🔑 Key Takeaways

1. **`MemberwiseClone()` is shallow** — it copies reference addresses, not the objects behind them
2. **Deep clone = shallow clone + replace every mutable reference field**
3. **The Registry stores its own clone** — external mutation of the original after `Register()` has no effect
4. **Every `Spawn()` returns a fresh clone** — callers can mutate freely without corrupting the registry
5. **In .NET, `ICloneable` is legacy** — prefer an explicit interface with named shallow/deep methods

## 📖 Further Reading

- "Design Patterns: Elements of Reusable Object-Oriented Software" (Gang of Four) — Chapter 3
- "Head First Design Patterns" — Chapter 4

---

← **Previous Pattern:** [1.4 - Builder](../1.4-Builder/)
→ **Next Pattern:** [1.6 - Object Pool](../1.6-ObjectPool/)
