# 3.06 — Memento Pattern

## Intent

Capture an object's internal state into an external object (the **Memento**) so the state can be restored later — without exposing the object's internals to the outside world.

---

## The Problem It Solves

You want to implement undo, rollback, or save/load — but the object's state is private. If you expose getters and setters for every field just for this purpose, you break encapsulation; any caller can now read and write internal state at will.

**Memento solves this by giving the Originator full control:**

- Only the Originator creates mementos (`Save()`)
- Only the Originator reads memento state to restore (`Restore()`)
- The Caretaker holds the memento but treats it as an opaque token — it never inspects the contents

---

## Solution: Game Checkpoint System

A game character saves their state before dangerous encounters and restores it if they die.

### Participants

| Role | Class | Responsibility |
|------|-------|---------------|
| **Originator** | `GameCharacter` | Creates mementos from its own state; restores from them |
| **Memento** | `CharacterMemento` | Immutable snapshot; `internal` constructor — only `GameCharacter` can create one |
| **Caretaker** | `CheckpointHistory` | Holds the stack of mementos; never reads their contents |

### State captured in each memento

| Field | Type | Description |
|-------|------|-------------|
| `Health` | `int` | Current hit points (0–100) |
| `Mana` | `int` | Current mana (0–100) |
| `Position` | `Position` | X/Y coordinates in the world |
| `Level` | `int` | Character level |
| `Inventory` | `IReadOnlyList<string>` | Deep copy of held items |
| `Label` | `string` | Human-readable checkpoint name |
| `SavedAt` | `DateTime` | When the snapshot was taken |

---

## Structure

```
MementoPattern/
├── GameCharacter.cs      ← Originator: Save() and Restore()
├── CharacterMemento.cs   ← Memento: immutable snapshot, internal constructor
├── CheckpointHistory.cs  ← Caretaker: stack of mementos, never inspects them
└── Position.cs           ← readonly record struct for X/Y coordinates
```

---

## Key Code

### Originator — creates and consumes mementos

```csharp
public sealed class GameCharacter
{
    private readonly List<string> _inventory = [];
    public int Health { get; private set; }
    // ...

    // Creates a deep-copy snapshot
    public CharacterMemento Save(string label = "")
        => new(Health, Mana, Position, Level, [.. _inventory], label, DateTime.Now);

    // Overwrites live state from a snapshot
    public void Restore(CharacterMemento memento)
    {
        Health   = memento.Health;
        Mana     = memento.Mana;
        Position = memento.Position;
        Level    = memento.Level;
        _inventory.Clear();
        _inventory.AddRange(memento.Inventory);
    }
}
```

### Memento — immutable; only the Originator can create one

```csharp
public sealed class CharacterMemento
{
    // internal constructor — external callers cannot new this up
    internal CharacterMemento(int health, int mana, Position position,
                               int level, IReadOnlyList<string> inventory,
                               string label, DateTime savedAt) { ... }

    public int                   Health    { get; }
    public int                   Mana      { get; }
    public Position              Position  { get; }
    public int                   Level     { get; }
    public IReadOnlyList<string> Inventory { get; }
    public string                Label     { get; }
    public DateTime              SavedAt   { get; }
}
```

### Caretaker — holds the stack; never opens the memento

```csharp
public sealed class CheckpointHistory
{
    private readonly Stack<CharacterMemento> _stack = new();

    public bool             CanUndo => _stack.Count > 0;
    public void             Push(CharacterMemento m) => _stack.Push(m);
    public CharacterMemento Pop()                    => _stack.Pop();
}
```

### Usage

```csharp
var hero    = new GameCharacter("Aria");
var history = new CheckpointHistory();

// Save before the boss fight
history.Push(hero.Save("Before boss"));

// Fight — take damage
hero.TakeDamage(80);   // HP: 20

// Died — restore checkpoint
hero.Restore(history.Pop());  // HP: 100
```

---

## Deep Copy — Why It Matters

`Save()` copies the inventory list with `[.. _inventory]`. Without this, the memento would hold a reference to the live list — any item picked up after saving would silently appear in the snapshot:

```csharp
hero.PickUp("Sword");
var m = hero.Save("snap");         // m.Inventory = ["Sword"]

hero.PickUp("Axe");                // live list is now ["Sword", "Axe"]
// m.Inventory is still ["Sword"] — deep copy protected it
```

The same applies in `Restore()` — `_inventory.AddRange(memento.Inventory)` copies the snapshot items into a fresh list, so future pickups don't corrupt the memento.

---

## Demo Scenarios

```
DEMO 1 — Basic save and restore
  Save at town entrance; take heavy damage; restore → back to full health

DEMO 2 — Multiple checkpoints through a dungeon
  Save at 3 points; die to final boss; restore most recent checkpoint

DEMO 3 — Rolling back through all checkpoints
  Repeatedly pop and restore until stack is empty

DEMO 4 — Deep copy independence
  Modify inventory after save; prove memento is unchanged

DEMO 5 — Checkpoint labels and metadata
  Show all saved checkpoints with label, level, HP, position, timestamp
```

---

## When to Use

- Implementing **undo/redo** when state is complex (not just operation-based)
- **Save/load** in games or long-running forms
- **Snapshots** before risky transactions (config changes, migrations)
- **Optimistic UI** — save state before an edit, restore if the user cancels

---

## When NOT to Use

- The object's state is huge — each snapshot copies everything, memory cost adds up
- The state changes constantly — snapshotting every change becomes expensive
- You only need to undo the last operation — Command with `Undo()` is cheaper

---

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Encapsulation preserved** | Originator's internals never exposed; Caretaker treats mementos as opaque |
| **Clean separation** | Originator handles "what to save"; Caretaker handles "when and how many" |
| **Simple Caretaker** | No logic in CheckpointHistory — just a stack |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **Memory** | Each snapshot is a full copy; many snapshots = high RAM usage |
| **Copy cost** | Deep copying large state on every save can be slow |
| **Stale references** | If state contains object references, deep copy is required (not just shallow) |

---

## Memento vs Command

Both enable undo — but they work differently:

| | Memento | Command |
|---|---------|---------|
| **What is stored** | A full state snapshot | The operation and enough info to reverse it |
| **Memory cost** | Proportional to object state size | Proportional to operation parameters only |
| **Undo mechanism** | Overwrite state with snapshot | Call `Undo()` method on the command |
| **Best for** | Complex state that's hard to reverse-engineer | Operations with well-defined inverses (insert → delete) |

Example: a text editor with 10,000 characters. Memento saves the whole string on every keystroke (expensive). Command saves only the character typed and its position (cheap).

In practice they can combine: use Command for fine-grained operations, Memento for coarse-grained "save a checkpoint here" moments.

---

## Related Patterns

- **Command (3.02)** — alternative undo mechanism; stores operations instead of state
- **Iterator (3.04)** — can use Memento to capture iterator position for later resumption
- **State (3.08)** — state objects can be saved as mementos for rollback

---

## Running the Demo

```bash
cd src/3-Behavioral/3.06-Memento/MementoPattern
dotnet run
```

## Running the Tests

```bash
cd src/3-Behavioral/3.06-Memento/MementoPattern.Tests
dotnet test
```
