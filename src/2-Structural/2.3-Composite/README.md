# 2.3 Composite Pattern

## Intent

Compose objects into tree structures to represent part-whole hierarchies. Composite lets clients treat individual objects and compositions of objects uniformly.

---

## The Problem It Solves

You have a tree structure — files inside directories, which can themselves be inside other directories. A caller wants to know the total size of "whatever is at this path." Without Composite, every caller must type-check:

```csharp
// Without Composite — type checking scattered everywhere
if (entry is File f)
    total += f.Size;
else if (entry is Directory d)
    total += d.RecursiveSum();  // different method, different call
```

With Composite, the recursive logic is encapsulated once inside the composite node itself. Every caller uses one uniform interface:

```csharp
// With Composite — always the same call
total += entry.GetSize();   // works for File, Directory, or any future node type
```

---

## Domain: File System

| Role | Class | Description |
|------|-------|-------------|
| **Component Interface** | `IFileSystemEntry` | Uniform contract: `Name`, `GetSize()`, `Display()`, `Search()` |
| **Leaf** | `File` | A single file — has a fixed size, no children |
| **Composite** | `Directory` | Contains `IFileSystemEntry` children; aggregates their sizes recursively |

---

## Structure

```
IFileSystemEntry
    ▲
    ├── File          (leaf)        — answers only for itself
    └── Directory     (composite)   — holds children, delegates + aggregates

Directory children: List<IFileSystemEntry>
                          ▲
                   can be File or Directory
```

The recursive call in `Directory.GetSize()`:

```csharp
public long GetSize() => _children.Sum(c => c.GetSize());
//                                         ↑
//                       each child may itself recurse further
```

`Directory` never checks whether a child is a `File` or another `Directory`. It just calls `GetSize()` on the interface. The leaf or the composite handles it from there.

---

## Key Design Decision: Where does child management live?

Two approaches exist:

**Option A — On the component interface (this implementation)**
```csharp
// IFileSystemEntry has no Add/Remove — only Directory does
dir.Add(file);   // only valid on Directory
```
Type-safe: you can't call `Add` on a `File`. The trade-off is the caller must have a `Directory` reference (not `IFileSystemEntry`) to add children.

**Option B — On the component interface**
```csharp
// IFileSystemEntry declares Add/Remove
// File throws NotSupportedException
entry.Add(child);  // compiles on anything, but may throw at runtime
```
More uniform, but sacrifices compile-time safety. Option A is generally preferred in C#.

---

## When to Use

- Your data is naturally tree-shaped: file systems, UI hierarchies, org charts, expression trees, scene graphs, menus
- You want callers to ignore the difference between a single object and a group
- Recursive operations (sum, search, render) should be encapsulated in the tree itself, not in every caller

## When NOT to Use

- Your structure is flat — Composite adds complexity for no gain
- Leaf and composite behaviour are too different to share a meaningful interface
- You need fine-grained control over which operations are valid on leaves vs composites (a plain type hierarchy may be cleaner)

---

## Benefits

- Eliminates type-checking (`is File` / `is Directory`) at every call site
- Recursive operations are written once, inside the composite — not in every consumer
- New node types (e.g. `SymbolicLink`) can be added without changing existing code
- Any subtree can be treated as the root — composable by definition

## Drawbacks

- Hard to restrict what can be added to a composite (you can add any `IFileSystemEntry`)
- If leaves and composites have very different responsibilities, forcing a common interface can feel artificial

---

## Running the Demo

```bash
cd src/2-Structural/2.3-Composite/CompositePattern
dotnet run
```

## Running Tests

```bash
cd src/2-Structural/2.3-Composite/CompositePattern.Tests
dotnet test
```

---

## Related Patterns

- **Iterator** — often used with Composite to traverse a tree without exposing its structure
- **Visitor** — lets you add operations across a Composite tree without modifying node classes
- **Decorator** — also uses recursive composition, but wraps a single object to add behaviour rather than building a tree

---

### Composite vs Iterator

Iterator provides sequential traversal *over* a collection — it doesn't care whether elements are nested or flat. Composite builds the tree structure; Iterator then walks it.

```csharp
// Composite builds the tree
var root = new Directory("root");
root.Add(new File("a.txt", 100));
root.Add(new Directory("sub").Add(new File("b.txt", 200)));

// Iterator (LINQ / foreach) walks whatever the Composite exposes
// Search() is essentially a recursive iterator built into the Composite
foreach (var entry in root.Search(e => e is File))
    Console.WriteLine(entry.Name);   // a.txt, b.txt

// Without the Composite's built-in Search, you'd write a separate
// tree-walker yourself — that external walker is the Iterator role.
```

**When to reach for Iterator separately:** when the traversal order (pre-order, post-order, breadth-first) needs to vary at runtime, extract it into a standalone iterator class rather than hardcoding it inside `Directory`.

---

### Composite vs Visitor

Visitor adds a *new operation* to every node in the tree without touching node classes. Compare to just adding a method on the interface:

```csharp
// Adding a new operation WITHOUT Visitor — must edit every node class
// File.cs:      public long CountLines() => ...
// Directory.cs: public long CountLines() => _children.Sum(c => c.CountLines());

// With Visitor — new operation lives in one place, nodes stay unchanged
public interface IFileSystemVisitor
{
    void Visit(File file);
    void Visit(Directory directory);
}

public class SizeAuditVisitor : IFileSystemVisitor
{
    public long TotalWasted { get; private set; }

    public void Visit(File file)
    {
        // Flag files that are suspiciously large
        if (file.GetSize() > 100_000_000) TotalWasted += file.GetSize();
    }

    public void Visit(Directory directory) { /* aggregate if needed */ }
}

// Nodes call visitor.Visit(this) — double dispatch picks the right overload
```

**Rule of thumb:** if you anticipate adding many *new operations* but rarely new node types, use Visitor. If you anticipate adding new node types frequently, keep operations on the interface (Composite alone) so you don't have to update every Visitor.

---

### Composite vs Decorator

Both use recursive composition, but for opposite purposes:

| | Composite | Decorator |
|---|---|---|
| **Goal** | Build a *tree* of objects treated uniformly | Wrap *one* object to add behaviour |
| **Children** | Many children (0..N) | Always exactly one wrapped object |
| **Adds behaviour?** | No — delegates unchanged | Yes — adds/modifies behaviour before or after |
| **Example** | `Directory` summing its children | `LoggingStream` wrapping a `FileStream` |

```csharp
// Composite — Directory holds many children, aggregates their sizes
var dir = new Directory("src");
dir.Add(new File("a.cs", 1000))
   .Add(new File("b.cs", 2000));
Console.WriteLine(dir.GetSize());   // 3000 — sum of children

// Decorator — wraps one object, adds behaviour (logging)
// Both LoggingStream and FileStream implement IStream
var stream = new LoggingStream(new FileStream("data.bin"));
stream.Write(bytes);   // LoggingStream logs, then delegates to FileStream
// GetSize() equivalent would return the inner stream's size unchanged
```

If you see "has children → aggregates" it's Composite. If you see "wraps one thing → enhances" it's Decorator.
