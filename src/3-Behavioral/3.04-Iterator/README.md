# 3.04 Iterator Pattern

## Intent

Provide a way to access elements of a collection sequentially without exposing its underlying representation. The iterator encapsulates the traversal logic so different traversal strategies can be swapped without changing the collection.

---

## The Problem It Solves

Without Iterator, the client must know the internal structure of the collection to traverse it, and adding a new traversal order (e.g., shuffle) requires changing the collection itself:

```csharp
// Without Iterator — client knows the internals; shuffle requires changing Playlist
for (int i = 0; i < playlist.Songs.Count; i++)
    Play(playlist.Songs[i]);

// Adding shuffle forces a new method on Playlist:
playlist.Shuffle();  // mutates the list — affects all other users of that list
for (int i = 0; i < playlist.Songs.Count; i++)
    Play(playlist.Songs[i]);
```

With Iterator, traversal strategies are separate objects. The collection is never modified:

```csharp
IPlaylistIterator iter = playlist.GetShuffleIterator();
while (iter.HasNext)
    Play(iter.Next());  // playlist unchanged; shuffle is in the iterator
```

---

## Domain: Music Playlist

| Role | Class | Description |
|------|-------|-------------|
| **Aggregate** | `Playlist` | Holds songs; creates iterators |
| **Iterator interface** | `IPlaylistIterator` | `HasNext`, `Next()`, `Reset()` |
| **Concrete iterator** | `SequentialIterator` | Songs in original insertion order |
| **Concrete iterator** | `ShuffleIterator` | Fisher-Yates shuffle at construction; deterministic with a seed |
| **Concrete iterator** | `FilterIterator` | Only songs matching a predicate |

---

## Structure

```
Playlist (aggregate)
  │
  ├── GetSequentialIterator() ──▶ SequentialIterator
  ├── GetShuffleIterator()    ──▶ ShuffleIterator
  └── GetFilterIterator(pred) ──▶ FilterIterator

All three implement IPlaylistIterator:
  HasNext : bool
  Next()  : Song
  Reset() : void
```

Multiple iterators can be active over the same playlist simultaneously — each holds its own position independently.

---

## The Iterator Interface

```csharp
public interface IPlaylistIterator
{
    bool HasNext { get; }   // true while unconsumed items remain
    Song Next();            // returns next item and advances position
    void Reset();           // returns iterator to the beginning
}
```

Standard usage pattern:

```csharp
var iter = playlist.GetSequentialIterator();
while (iter.HasNext)
{
    var song = iter.Next();
    Console.WriteLine(song.Title);
}
```

---

## Multiple Iterators, One Collection

The same playlist can be iterated in multiple ways at the same time. Each iterator is independent:

```csharp
var sequential = playlist.GetSequentialIterator();
var shuffled   = playlist.GetShuffleIterator(seed: 42);
var rockOnly   = playlist.GetFilterIterator(s => s.Genre == "Rock");

// All three iterate the same songs; playlist is unchanged; positions are independent
sequential.Next();  // advances only sequential
shuffled.Next();    // advances only shuffled
```

---

## Iterator Pattern in C#: `IEnumerable<T>` and `yield return`

C# builds the Iterator pattern into the language. `IEnumerable<T>` is the aggregate interface; `IEnumerator<T>` is the iterator interface. The `foreach` loop calls `GetEnumerator()`, then `MoveNext()` and `Current` on the result.

`yield return` lets you write an iterator as a simple method — the compiler generates the full `IEnumerator<T>` state machine behind the scenes:

```csharp
// This method compiles to a class that implements IEnumerator<Song>
public IEnumerable<Song> AsEnumerable()
{
    foreach (var song in _songs)
        yield return song;   // pauses here, hands out one song, resumes on next MoveNext()
}

// Consumed with foreach — no explicit HasNext/Next needed
foreach (var song in playlist.AsEnumerable())
    Console.WriteLine(song.Title);
```

The `yield return` approach and the manual `IPlaylistIterator` approach are the same pattern — C# just handles the boilerplate for you when you use `yield return`.

---

## External vs Internal Iterators

| | External (this implementation) | Internal (LINQ / `foreach`) |
|---|---|---|
| **Who controls the loop** | The caller (`while (iter.HasNext)`) | The iterator itself |
| **Flexibility** | Can pause, skip, mix two iterators | Simpler — just provide a body |
| **Example** | `while (iter.HasNext) iter.Next()` | `playlist.AsEnumerable().Where(...)` |

---

## When to Use

- You need multiple traversal strategies for the same collection
- You want to decouple traversal logic from the collection
- You need multiple independent positions over the same collection at the same time

## When NOT to Use

- A simple `foreach` or LINQ query is sufficient — `yield return` gives you all the benefits with far less code
- The collection has only one obvious traversal order and will never need another

---

## Benefits

- Collection and traversal are separate — adding a new iterator doesn't change the collection
- Multiple iterators can coexist over the same collection independently
- Traversal algorithm is hidden from the client

## Drawbacks

- More classes than a simple `foreach` loop
- Manual iterators are largely superseded by `IEnumerable<T>` + `yield return` in C#

---

## Running the Demo

```bash
cd src/3-Behavioral/3.04-Iterator/IteratorPattern
dotnet run
```

## Running Tests

```bash
cd src/3-Behavioral/3.04-Iterator/IteratorPattern.Tests
dotnet test
```

---

## Related Patterns

- **Composite** — Iterator is commonly used to traverse Composite trees; a recursive iterator walks the whole hierarchy
- **Visitor** — alternative for "do something to every element"; Visitor separates the operation from the structure rather than the traversal from the structure
- **Factory Method** — the aggregate (`Playlist`) uses Factory Method to create the right iterator

---

### Iterator vs LINQ in C#

In practice, custom `IPlaylistIterator` classes are rarely written in C# because `IEnumerable<T>` + LINQ covers most traversal needs:

```csharp
// Custom FilterIterator:
var iter = playlist.GetFilterIterator(s => s.Genre == "Rock");

// Equivalent with LINQ — same result, zero extra classes:
var rockSongs = playlist.Songs.Where(s => s.Genre == "Rock");

// Custom ShuffleIterator:
var iter = playlist.GetShuffleIterator();

// Equivalent with LINQ + OrderBy:
var shuffled = playlist.Songs.OrderBy(_ => Random.Shared.Next());
```

The custom iterator is still valuable when:
- The traversal is stateful and complex (e.g., depends on external events)
- You need `Reset()` semantics
- You need multiple named strategies switchable at runtime via an interface
