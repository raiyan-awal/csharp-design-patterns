# 4.01 — Repository Pattern

## Intent

Mediate between the domain layer and the data mapping layer using a collection-like interface. Business logic calls the repository; it never knows whether there is a SQL database, a document store, or an in-memory list on the other side.

---

## The Problem It Solves

Without Repository, data access leaks into service classes and couples business logic directly to the storage technology:

```csharp
// WITHOUT Repository — SQL mixed into business logic:
public class ProductService
{
    private readonly SqlConnection _db;

    public async Task<Product?> GetFeaturedProduct()
    {
        var cmd = new SqlCommand("SELECT TOP 1 * FROM Products WHERE IsActive=1 ORDER BY Price DESC", _db);
        var reader = await cmd.ExecuteReaderAsync();
        return reader.Read() ? MapProduct(reader) : null;
    }

    public async Task DiscountElectronics(decimal pct)
    {
        // Switching to MongoDB next sprint? Every method needs rewriting.
        await _db.ExecuteAsync("UPDATE Products SET Price = Price * @pct WHERE Category='Electronics'", new { pct });
    }
}
```

The service can't be unit-tested without a real database. Changing storage technology means rewriting every method. Repository puts an interface in between so that business logic depends on an abstraction, not on ADO.NET or any specific ORM.

---

## Solution: Generic Product Repository

A generic `IRepository<T>` interface covers standard CRUD and querying. Two implementations ship with this demo:

- `InMemoryProductRepository` — thread-safe in-memory store with optional seed data; perfect for tests and demos
- `SqlProductRepository` — Dapper + `IDbConnection`; runs against SQLite for the demo, SQL Server in production

### Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| **Repository interface** | `IRepository<T>` | CRUD + query contract; what callers depend on |
| **In-memory impl** | `InMemoryProductRepository` | Thread-safe in-memory implementation |
| **SQL impl** | `SqlProductRepository` | Dapper over `IDbConnection` — SQLite or SQL Server |
| **Entity** | `Product` | Persistence-ignorant domain object |

---

## Structure

```
RepositoryPattern/
├── IRepository.cs                  ← generic interface: 8 async methods
├── Product.cs                      ← domain entity (no database attributes)
├── InMemoryProductRepository.cs    ← concrete: thread-safe List<T> with locking
└── SqlProductRepository.cs         ← concrete: Dapper over IDbConnection
```

---

## Key Code

### Repository interface — callers depend only on this

```csharp
public interface IRepository<T> where T : class
{
    Task<T?>              GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task                 AddAsync(T entity);
    Task                 UpdateAsync(T entity);
    Task                 DeleteAsync(int id);
    Task<bool>           ExistsAsync(int id);
    Task<int>            CountAsync();
}
```

### Entity — persistence-ignorant

```csharp
// No [Table], no [Column], no [Key] — the entity knows nothing about storage
public sealed class Product
{
    public int     Id            { get; set; }
    public string  Name          { get; set; } = "";
    public string  Category      { get; set; } = "";
    public decimal Price         { get; set; }
    public int     StockQuantity { get; set; }
    public bool    IsActive      { get; set; } = true;
}
```

### Usage — declared as the interface

```csharp
// Composition root — the ONLY place that knows about the concrete type.
// Change this one line; nothing else in the application changes.
IRepository<Product> repo = new InMemoryProductRepository(seedData: true);
// or:
IRepository<Product> repo = new SqlProductRepository(new SqlConnection("Server=...;"));
// or:
IRepository<Product> repo = new SqlProductRepository(new SqliteConnection("Data Source=:memory:"));

var product   = await repo.GetByIdAsync(1);
var all       = await repo.GetAllAsync();
var expensive = await repo.FindAsync(p => p.Price > 500m);

await repo.AddAsync(new Product { Name = "AirPods Pro", Price = 329.99m });
await repo.UpdateAsync(product);
await repo.DeleteAsync(7);
```

---

## Expression\<Func\<T, bool\>\> vs Func\<T, bool\>

`FindAsync` uses `Expression<Func<T, bool>>` rather than the simpler `Func<T, bool>`:

| | `Func<T, bool>` | `Expression<Func<T, bool>>` |
|---|---|---|
| In-memory | Works directly | `.Compile()` → works |
| EF Core | Loads **all rows**, filters in-memory | Translated to a SQL `WHERE` clause |
| Dapper | Loads all rows, filters in-memory | Same (no translator) |

The calling code is identical either way — a lambda `p => p.Price > 500m` is implicitly convertible to both types. The interface uses `Expression<Func<T, bool>>` so that an EF Core implementation can translate the predicate to SQL automatically:

```csharp
// EF Core implementation — one line, full SQL translation:
public Task<IEnumerable<Product>> FindAsync(Expression<Func<Product, bool>> predicate)
    => _context.Products.Where(predicate).ToListAsync();

// InMemory implementation — compile the expression, then apply:
public Task<IEnumerable<Product>> FindAsync(Expression<Func<Product, bool>> predicate)
    => Task.FromResult([.._products.Where(predicate.Compile())]);
```

---

## SQL Repository: swapping in Dapper

`SqlProductRepository` accepts any `IDbConnection`, making it provider-agnostic:

```csharp
// SQL Server in production:
IRepository<Product> repo = new SqlProductRepository(new SqlConnection("Server=...;Database=Shop;"));

// SQLite for local dev / CI — no server required:
IRepository<Product> repo = new SqlProductRepository(new SqliteConnection("Data Source=products.db"));
```

`AddAsync` runs a parameterized `INSERT` and reads back `last_insert_rowid()` (SQLite) or `SCOPE_IDENTITY()` (SQL Server) to assign the generated ID to the entity. All other methods follow the same pattern: one parameterized Dapper call per operation.

---

## Demo Scenarios

```
PROBLEM  — shows SQL embedded directly in a service method
DEMO 1   — GetByIdAsync (found + not found) and GetAllAsync
DEMO 2   — AddAsync: repository assigns the ID automatically
DEMO 3   — UpdateAsync: modify a retrieved entity and persist
DEMO 4   — DeleteAsync + ExistsAsync
DEMO 5   — FindAsync with three different predicates (category, price, stock)
DEMO 6   — the interface is the contract; swap implementations without changing callers
DEMO 7   — SqlProductRepository: same interface, identical calling code, real SQL
```

---

## When to Use

- Business logic is complex and must be unit-tested independently of the database
- The application might switch storage backends (SQL to NoSQL, or add a caching layer)
- You are applying Domain-Driven Design — entities should not know about persistence
- You want to centralise all data access logic for a given entity type

---

## When NOT to Use

- Simple CRUD applications — Entity Framework's `DbSet<T>` already acts as a repository
- When the abstraction adds no value (you're wrapping EF one-to-one with no extra logic)
- Very simple data access needs where the indirection costs more than it saves

---

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Persistence ignorance** | Domain code never imports a database library |
| **Testability** | Swap in `InMemoryProductRepository` for fast, isolated unit tests |
| **Swappable backend** | One-line change at the composition root to switch implementations |
| **Centralised queries** | Complex queries are defined once in the repository, not scattered across services |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **Leaky abstraction** | `Expression<Func<T, bool>>` vs raw SQL exposes ORM assumptions through the interface |
| **Method proliferation** | Specific queries (by email, by date range) add up to many small methods |
| **Redundant with EF Core** | `DbSet<T>` + `DbContext` already implement the pattern — an extra wrapper can be overkill |

---

## Related Patterns

- **Unit of Work (4.02)** — coordinates multiple repository operations into a single transaction
- **Specification (4.04)** — encapsulates query predicates as objects, solving the method-per-query problem
- **Proxy (2.7)** — a caching repository is structurally a Proxy wrapping the real repository
- **Facade (2.5)** — the repository is a Facade over the data mapping layer

---

## Running the Demo

```bash
cd src/4-Enterprise/4.01-Repository/RepositoryPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.01-Repository/RepositoryPattern.Tests
dotnet test
```
