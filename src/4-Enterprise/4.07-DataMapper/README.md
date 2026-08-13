# 4.07 — Data Mapper

## Intent

A Data Mapper moves data between domain objects and a database while keeping each completely unaware of the other. The domain object contains only business logic and knows nothing about SQL or table layout. A separate mapper class knows both structures and translates between them in both directions.

## The Problem It Solves

Without a Data Mapper, domain objects carry their own persistence logic — a style called Active Record:

```csharp
// Active Record — the domain object knows about the database
public class Film
{
    private static SqliteConnection _db = new(connectionString);

    public string Title { get; set; } = "";
    public string Director { get; set; } = "";

    public static Film? Find(int id) =>          // DB knowledge inside domain
        _db.QuerySingle<Film>("SELECT * FROM Films WHERE Id = @id", new { id });

    public void Save() =>                        // DB knowledge inside domain
        _db.Execute("INSERT INTO Films ...", this);
}
```

Problems this creates:

- **Mixed concerns** — business logic and SQL live in the same class; changing either risks breaking the other
- **Coupled evolution** — rename a column and you edit the domain object; add a domain rule and you wade through SQL
- **Hard to test** — instantiating the object requires a live database connection
- **No substitutability** — you cannot swap the storage engine without rewriting the domain class

## Solution: Separate Domain Object from Mapper

The domain object is a plain C# class with no storage knowledge. A dedicated mapper class holds all the SQL and converts rows to domain objects and back:

```csharp
// Pure domain — no SQL, no attributes, no base class
public sealed class Film
{
    public int Id { get; init; }
    public string Title { get; init; }
    public bool CertifiedFresh { get; private set; }

    public void Certify()   => CertifiedFresh = true;
    public void Decertify() => CertifiedFresh = false;
}

// Mapper — owns all SQL, translates rows ↔ domain
public sealed class FilmMapper(IDbConnection db) : IMapper<Film>
{
    public Film? FindById(int id) { /* SQL here */ }
    public Film  Insert(Film film) { /* SQL here */ }
    public void  Update(Film film) { /* SQL here */ }
    public void  Delete(int id)   { /* SQL here */ }
}
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| **Domain Object** | `Film`, `Review` | Business logic and invariants; no knowledge of storage |
| **Mapper Interface** | `IMapper<T>` | Contract for CRUD operations on a domain type |
| **Concrete Mapper** | `FilmMapper`, `ReviewMapper` | Translates between domain objects and database rows using Dapper |
| **Private DTO** | `FilmRow`, `ReviewRow` (inner classes) | Dapper-friendly plain types that the mapper fills from query results, then converts to domain |
| **Schema** | `Schema.Create(db)` | Creates the database tables; kept separate from both domain and mapper |

## Structure

```
4.07-DataMapper/
├── DataMapperPattern/
│   ├── Domain/
│   │   ├── Film.cs          ← pure domain: title, director, genre, Certify()/Decertify()
│   │   └── Review.cs        ← pure domain: score 1–10, validated in constructor
│   ├── Mappers/
│   │   ├── IMapper.cs       ← generic CRUD interface
│   │   ├── FilmMapper.cs    ← SQL ↔ Film; inner FilmRow DTO; FindByGenre/FindByDirector
│   │   ├── ReviewMapper.cs  ← SQL ↔ Review; FindByFilmId, AverageScore
│   │   └── Schema.cs        ← DDL helper: CREATE TABLE Films / Reviews
│   └── Program.cs
└── DataMapperPattern.Tests/
    └── DataMapperTests.cs   ← 20 tests; SQLite :memory:
```

## Key Code

### Pure Domain Object

```csharp
public sealed class Film
{
    public int Id { get; init; }
    public string Title { get; init; }
    public string Director { get; init; }
    public string Genre { get; init; }
    public int ReleaseYear { get; init; }
    public int RuntimeMinutes { get; init; }
    public bool CertifiedFresh { get; private set; }

    public Film(int id, string title, string director, string genre,
                int releaseYear, int runtimeMinutes, bool certifiedFresh = false) { ... }

    public void Certify()   => CertifiedFresh = true;
    public void Decertify() => CertifiedFresh = false;
}
```

`Film` has no `[Column]` attributes, no `Save()` method, no database field. It can be created and tested with `new Film(...)` — no infrastructure needed.

### Private DTO — Dapper Bridge

```csharp
// Inside FilmMapper — never visible outside the mapper
private sealed class FilmRow
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Director { get; init; } = "";
    public string Genre { get; init; } = "";
    public int ReleaseYear { get; init; }
    public int RuntimeMinutes { get; init; }
    public bool CertifiedFresh { get; init; }

    public Film ToDomain() =>
        new(Id, Title, Director, Genre, ReleaseYear, RuntimeMinutes, CertifiedFresh);
}
```

`FilmRow` is what Dapper fills from a query result. `ToDomain()` converts it to the rich domain object. If the DB schema changes (column rename, new column), only `FilmRow` and the SQL strings need updating — `Film` is untouched.

### Mapper Implementation

```csharp
public Film? FindById(int id)
{
    var row = db.QuerySingleOrDefault<FilmRow>(
        "SELECT Id, Title, Director, Genre, ReleaseYear, RuntimeMinutes, CertifiedFresh " +
        "FROM Films WHERE Id = @id", new { id });
    return row?.ToDomain();
}

public Film Insert(Film film)
{
    var id = db.ExecuteScalar<int>(
        "INSERT INTO Films (Title, Director, Genre, ReleaseYear, RuntimeMinutes, CertifiedFresh) " +
        "VALUES (@Title, @Director, @Genre, @ReleaseYear, @RuntimeMinutes, @CertifiedFresh); " +
        "SELECT last_insert_rowid();",
        new { film.Title, film.Director, film.Genre,
              film.ReleaseYear, film.RuntimeMinutes, film.CertifiedFresh });
    return film.WithId(id);
}
```

The mapper selects only the columns it knows about. Extra columns added to the table in future migrations are ignored automatically — the domain object never sees them.

## Demo Scenarios

```
── The Problem Without Data Mapper ──────────────────────────────────────────────
  Shows Active Record style: SQL embedded in the domain class.
  Explains why mixing concerns makes both harder to change.

── Demo 1: Pure Domain Objects ──────────────────────────────────────────────────
  Creates Film objects in memory with new Film(...) — no DB connection needed.
  Calls Certify() and Decertify() to show domain logic works independently.

── Demo 2: Inserting via the Mapper ─────────────────────────────────────────────
  Inserts 5 Canadian films (Atanarjuat, Incendies, The Sweet Hereafter, etc.).
  Mapper returns Film with assigned Id; lists all films ordered by release year.

── Demo 3: Querying ─────────────────────────────────────────────────────────────
  FindById — found and not-found cases.
  FindByGenre("Drama") and FindByDirector("Denis Villeneuve").

── Demo 4: Updating and Deleting ────────────────────────────────────────────────
  Certifies Incendies and updates via mapper; reloads to confirm persistence.
  Deletes Bon Cop, Bad Cop; verifies count drops.

── Demo 5: Reviews ──────────────────────────────────────────────────────────────
  Inserts reviews for two films via ReviewMapper.
  FindByFilmId per film; AverageScore computed in SQL.

── Demo 6: Schema Independence ──────────────────────────────────────────────────
  Adds an AddedOn audit column to the Films table via ALTER TABLE.
  FilmMapper still loads Film correctly — Film class unchanged.
  Demonstrates that domain and schema can evolve at different rates.
```

## When to Use

- Domain objects are rich with business logic that must remain clean and independently testable
- The database schema is likely to evolve separately from the domain model (added columns, renamed tables, schema migrations)
- Multiple database backends might be needed (SQLite in tests, SQL Server in production)
- You are following Domain-Driven Design and want the domain layer free of infrastructure concerns

## When NOT to Use

- Simple CRUD with little or no domain logic — Active Record is faster to write and easier to follow
- The team is small and the extra files add more overhead than value
- You are using an ORM like Entity Framework that provides its own mapping layer — layering Data Mapper on top creates unnecessary complexity

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Separation of concerns** | Domain logic and SQL are isolated; each can change without touching the other |
| **Testable domain** | `new Film(...)` works without a database; business rules are unit-testable in milliseconds |
| **Schema independence** | Add columns, rename tables, switch databases — only the mapper changes |
| **Clear translation point** | All data conversion happens in one place, making it easy to audit and debug |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **More files** | Every domain type needs a mapper class, a DTO, and SQL strings |
| **Boilerplate** | Insert/Update SQL must list every mapped column explicitly — tedious to maintain for wide tables |
| **No lazy loading** | Unlike an ORM, related objects must be loaded explicitly via their own mapper |
| **N+1 risk** | Loading a list of films then calling `FindByFilmId` per film results in N+1 queries; requires explicit join queries to avoid |

## Related Patterns

- **Active Record (4.08)** — the alternative: the object knows how to persist itself; simpler but couples domain to schema
- **Repository (4.01)** — commonly built on top of Data Mapper; the mapper handles row translation, the repository provides the collection-like interface
- **Unit of Work (4.02)** — coordinates multiple mapper calls in a single transaction
- **Service Layer (4.06)** — calls mappers (or repositories) to load and persist domain objects during use-case execution

## Running the Demo

```bash
cd src/4-Enterprise/4.07-DataMapper/DataMapperPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.07-DataMapper/DataMapperPattern.Tests
dotnet test
```
