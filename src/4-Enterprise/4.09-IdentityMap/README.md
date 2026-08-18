# 4.09 — Identity Map

## Intent

Identity Map ensures that each database row is loaded into memory only once per session. It keeps every loaded object in a dictionary keyed by its primary key. When code requests the same row a second time, the map returns the existing in-memory instance rather than re-querying the database, guaranteeing that a given row is always represented by exactly one object.

## The Problem It Solves

Without an Identity Map, every call to a mapper or repository creates a new object from the database row, even when the same row has already been loaded:

```csharp
// Two separate loads of the same row produce two separate objects
var mapperA = new ArtworkMapper(connection);
var mapperB = new ArtworkMapper(connection);

var fromA = mapperA.FindById(42);   // hits DB, creates object X
var fromB = mapperB.FindById(42);   // hits DB again, creates object Y

fromA.PutOnDisplay();

Console.WriteLine(fromA.OnDisplay);          // true
Console.WriteLine(fromB.OnDisplay);          // false — stale!
ReferenceEquals(fromA, fromB);               // false — different objects
```

Problems this creates:

- Two DB round trips for the same data waste I/O and inflate query counts under load.
- Two in-memory objects representing the same row can carry different state — a mutation on one is invisible to code holding the other, leading to subtle stale-data bugs.
- Dirty tracking ("what has changed?") becomes impossible — you cannot tell which of the two copies is authoritative.
- Any code that compares object identity (`ReferenceEquals`, collections, equality checks) breaks unpredictably.

## Solution: One Row, One Object

The Identity Map sits inside each mapper. Before querying the database, `FindById` checks the map. On a hit, the cached instance is returned immediately. On a miss, the row is loaded, stored in the map, and returned. Every subsequent request for the same Id returns the same object:

```csharp
// Single mapper instance — all code shares the same map
var artwork1 = mapper.FindById(42);  // hits DB → registers in map
var artwork2 = mapper.FindById(42);  // map hit → no DB query

artwork1.PutOnDisplay();
Console.WriteLine(artwork2.OnDisplay);       // true — same object
ReferenceEquals(artwork1, artwork2);         // true — guaranteed
Console.WriteLine(mapper.LoadCount);         // 1 — only one DB hit
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Identity Map | `IdentityMap<TKey, TEntity>` | Generic dictionary that stores and retrieves cached entities by key |
| Mapper | `ArtworkMapper` | Checks the map before querying; registers new loads; evicts on delete |
| Mapper | `ArtistMapper` | Same responsibility for the `Artist` entity |
| Domain | `Artwork` | Pure domain object; unaware of the map |
| Domain | `Artist` | Pure domain object; unaware of the map |
| Infrastructure | `Schema` | Creates the SQLite tables |

## Structure

```
4.09-IdentityMap/
├── IdentityMapPattern/
│   ├── Domain/
│   │   ├── Artwork.cs                ← pure domain; no DB knowledge
│   │   └── Artist.cs                 ← pure domain; no DB knowledge
│   ├── Infrastructure/
│   │   └── Schema.cs                 ← DDL for Artists and Artworks tables
│   ├── Mappers/
│   │   ├── IdentityMap.cs            ← generic map: Register / TryGet / Remove / Clear
│   │   ├── ArtworkMapper.cs          ← mapper with built-in IdentityMap<int, Artwork>
│   │   └── ArtistMapper.cs           ← mapper with built-in IdentityMap<int, Artist>
│   └── Program.cs                    ← 6-section demo
├── IdentityMapPattern.Tests/
│   └── IdentityMapTests.cs           ← 20 tests: 7 unit (IdentityMap) + 13 integration (mappers)
└── README.md
```

## Key Code

### The generic IdentityMap

The map itself is a thin, typed wrapper over a dictionary. It is not tied to any domain type or persistence technology — any mapper can embed one.

```csharp
public sealed class IdentityMap<TKey, TEntity> where TKey : notnull
{
    private readonly Dictionary<TKey, TEntity> _store = new();

    public bool TryGet(TKey key, out TEntity? entity) => _store.TryGetValue(key, out entity);
    public void Register(TKey key, TEntity entity)    => _store[key] = entity;
    public void Remove(TKey key)                      => _store.Remove(key);
    public bool Contains(TKey key)                    => _store.ContainsKey(key);
    public int  Count                                 => _store.Count;
    public void Clear()                               => _store.Clear();
}
```

### FindById — check map before querying

The map check is the first thing `FindById` does. `LoadCount` is incremented only when the database is actually queried, making it easy to verify in tests and demos that the map is working.

```csharp
public Artwork? FindById(int id)
{
    if (_map.TryGet(id, out var cached)) return cached;  // map hit — no DB

    LoadCount++;
    var row = _db.QuerySingleOrDefault<Row>("SELECT * FROM Artworks WHERE Id = @id", new { id });
    if (row is null) return null;

    var artwork = row.ToDomain();
    _map.Register(id, artwork);   // store for future requests
    return artwork;
}
```

### FindAll — respects the map for already-loaded objects

`FindAll` fetches all rows from the database, but for any row whose Id is already in the map it returns the existing in-memory instance rather than creating a new one. This preserves any unsaved in-memory state and keeps `ReferenceEquals` stable across find operations.

```csharp
public IReadOnlyList<Artwork> FindAll()
{
    var rows = _db.Query<Row>("SELECT * FROM Artworks ORDER BY Title");
    var result = new List<Artwork>();
    foreach (var row in rows)
    {
        if (_map.TryGet(row.Id, out var cached))
            result.Add(cached!);          // already loaded — reuse instance
        else
        {
            var artwork = row.ToDomain();
            _map.Register(row.Id, artwork);
            result.Add(artwork);
        }
    }
    return result;
}
```

### Eviction on delete

When a row is deleted from the database the corresponding entry is removed from the map so stale references cannot be returned by future `FindById` calls.

```csharp
public void Delete(int id)
{
    _db.Execute("DELETE FROM Artworks WHERE Id = @id", new { id });
    _map.Remove(id);   // evict from cache
}
```

## Demo Scenarios

```
=== Vancouver Art Gallery — Identity Map Demo ===

1. Seeding the Gallery          Insert 3 artists and 5 artworks; map is pre-populated on insert
2. Cost Without Identity Map    Two separate mappers load the same row into different objects;
                                mutation on one is invisible to the other
3. Identity Guarantee           Single mapper: two FindById calls return the same instance;
                                LoadCount stays at 1
4. In-Memory Consistency        PutOnDisplay() on one reference is visible through all references
                                to the same cached object
5. FindAll Populates the Map    FindAll loads 5 artworks; subsequent FindById hits the map,
                                not the database
6. Cache Eviction on Delete     Delete evicts the entry; FindById returns null
```

## When to Use

- You load the same rows repeatedly within a single request or unit of work and want to avoid redundant database queries.
- You need object identity guarantees — callers comparing object references must see the same instance for the same row.
- You are implementing dirty tracking or change detection, where you need a single authoritative in-memory copy of each row.
- You are building a session or unit-of-work scope and want to coordinate multiple mappers that load overlapping data.

## When NOT to Use

- The application is stateless (each request creates and discards its context immediately) — the map's benefit only materialises when the same row is loaded more than once within a scope.
- Memory is constrained — a long-lived map for a large dataset keeps all loaded objects alive for the lifetime of the scope.
- You need fresh data on every read — the map returns the cached (possibly stale) object; background updates to the database are not reflected until the map is cleared or the entry is evicted.
- The identity map is long-lived and shared across requests — a single shared map becomes a stale-data source and a concurrency hazard.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Eliminates duplicate DB hits | The same row is queried at most once per scope, regardless of how many callers request it |
| Object identity guarantee | `ReferenceEquals` is stable — every caller holding a reference to a given row holds the same object |
| In-memory consistency | A mutation made through one reference is immediately visible to all other references to the same entity |
| Foundation for dirty tracking | A single canonical instance makes it straightforward to detect what has changed before committing |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Memory growth | Every loaded entity stays alive as long as the map exists; large result sets increase the footprint of the scope |
| Stale reads | The map returns its cached version; database changes made by other sessions are invisible until the entry is evicted |
| Scope management | The map must be scoped correctly — per-request or per-unit-of-work; a global map is a shared-state hazard |
| No concurrency protection | The `Dictionary` inside `IdentityMap` is not thread-safe; concurrent access requires locking or a concurrent collection |

## Related Patterns

- **Data Mapper (4.07)** — Identity Map is commonly embedded inside a Data Mapper to add caching; the mapper in this pattern follows the same pure-domain approach as 4.07.
- **Unit of Work (4.02)** — a natural host for the identity map; the UoW creates the map at the start of a transaction and discards it on commit or rollback, bounding the map's lifetime correctly.
- **Repository (4.01)** — repositories can delegate to a shared identity map so that `FindById` calls across different repository instances return the same object for the same row.
- **Active Record (4.08)** — Active Record objects are self-persisting; adding an identity map to Active Record is unusual because there is no separate mapper layer to centralise the cache.
- **Lazy Load (4.10)** — often combined with Identity Map; Lazy Load defers loading a related object until it is accessed, and Identity Map ensures it is only loaded once when it is finally needed.

## Running the Demo

```bash
cd src/4-Enterprise/4.09-IdentityMap/IdentityMapPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.09-IdentityMap/IdentityMapPattern.Tests
dotnet test
```
