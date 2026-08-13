# 4.08 — Active Record

## Intent

Active Record is an architectural pattern that places both data-access logic and domain behaviour inside the same object. Each instance represents one row in a database table and knows how to read, write, and delete itself. The pattern was named and popularised by Martin Fowler in *Patterns of Enterprise Application Architecture* and forms the backbone of frameworks like Ruby on Rails.

## The Problem It Solves

Without Active Record, persistence code tends to leak throughout the application. Every feature that touches a rental unit must repeat the same SQL and repeat the same parameter mapping:

```csharp
// Scattered across the codebase — duplicated SQL, no shared business rules
void RentUnit(int id, SqliteConnection conn)
{
    conn.Execute("UPDATE RentalUnits SET IsAvailable = 0 WHERE Id = @Id", new { Id = id });
}

bool IsAlreadyRented(int id, SqliteConnection conn) =>
    conn.QuerySingle<int>("SELECT IsAvailable FROM RentalUnits WHERE Id = @Id", new { Id = id }) == 0;

void RaiseRent(int id, decimal amount, SqliteConnection conn)
{
    var current = conn.QuerySingle<string>("SELECT MonthlyRent FROM RentalUnits WHERE Id = @Id", new { Id = id });
    var newRent = decimal.Parse(current) + amount;
    conn.Execute("UPDATE RentalUnits SET MonthlyRent = @rent WHERE Id = @Id", new { rent = newRent.ToString("F2"), Id = id });
}
```

Problems with this approach:

- The business rule "can't rent an already-rented unit" is duplicated in every caller.
- SQL strings and column names are scattered — rename a column and you must find every callsite.
- There is no single place to add logging, validation, or auditing.
- Feature logic and infrastructure details are tangled from the start.

## Solution: The Domain Object Persists Itself

Each class encapsulates its own SQL. Domain methods like `Rent()` enforce the business rule *and* persist the change in one call. Static methods on the class serve as the query entry point:

```csharp
// Create and save in two explicit steps — Id is 0 until Save() is called
var unit = new RentalUnit("221 King St W, Apt 4A", "Toronto", "ON", 2_400m, 1);
unit.Save();                    // INSERT — Id is now assigned

// Domain behaviour: validates + persists in one call
unit.Rent();                    // throws if already rented, then saves IsAvailable = false
unit.UpdateRent(2_500m);        // validates positive, then saves new price

// Static finders — no separate repository class needed
var unit   = RentalUnit.FindById(42);
var avail  = RentalUnit.FindAvailable();
var local  = RentalUnit.FindByCity("Toronto");
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Active Record | `RentalUnit` | Wraps a rental unit row; owns all CRUD and domain rules |
| Active Record | `Tenant` | Wraps a tenant row; owns lease lifecycle |
| Infrastructure | `Database` | Holds the shared `IDbConnection` the records use |
| Infrastructure | `Schema` | Creates the SQLite tables on startup |

## Structure

```
4.08-ActiveRecord/
├── ActiveRecordPattern/
│   ├── Infrastructure/
│   │   ├── Database.cs          ← static connection holder; initialized once at startup
│   │   └── Schema.cs            ← DDL for RentalUnits and Tenants tables
│   ├── Records/
│   │   ├── RentalUnit.cs        ← active record: domain + CRUD + static finders
│   │   └── Tenant.cs            ← active record: lease lifecycle + CRUD + static finders
│   └── Program.cs               ← 6-section demo
├── ActiveRecordPattern.Tests/
│   └── ActiveRecordTests.cs     ← 21 tests; fresh in-memory DB per test
└── README.md
```

## Key Code

### Save() — INSERT or UPDATE based on Id

The `Id` starts at `0` (unsaved). `Save()` detects this to decide between INSERT and UPDATE. The newly assigned database Id is written back to the object immediately after INSERT.

```csharp
public void Save()
{
    if (Id == 0)
    {
        Id = Database.Connection.ExecuteScalar<int>(
            """
            INSERT INTO RentalUnits (Address, City, Province, MonthlyRent, ...)
            VALUES (@Address, @City, @Province, @MonthlyRent, ...);
            SELECT last_insert_rowid();
            """,
            new { Address, City, Province, MonthlyRent = MonthlyRent.ToString("F2"), ... });
    }
    else
    {
        Database.Connection.Execute(
            "UPDATE RentalUnits SET ... WHERE Id = @Id",
            new { Address, City, ..., Id });
    }
}
```

### Domain behaviour that auto-saves

`Rent()` enforces the business rule first, then mutates state, then persists — all in one call. The caller never touches SQL.

```csharp
public void Rent()
{
    if (!IsAvailable)
        throw new InvalidOperationException($"'{Address}' is already rented.");
    IsAvailable = false;
    LastUpdated = DateTime.UtcNow;
    Save();   // ← persists the state change immediately
}
```

### Static finder methods

Finders live on the class itself. Dapper maps the result rows into a private `Row` DTO (to sidestep public-setter requirements), which is then converted to a fully-encapsulated `RentalUnit` via a private constructor.

```csharp
public static IReadOnlyList<RentalUnit> FindAvailable()
{
    return Database.Connection
        .Query<Row>("SELECT * FROM RentalUnits WHERE IsAvailable = 1 ORDER BY MonthlyRent")
        .Select(r => r.ToUnit())
        .ToList();
}
```

### Private constructor for DB-to-domain mapping

The inner `Row` DTO holds raw database values. Its `ToUnit()` calls a `private` constructor on `RentalUnit` that accepts all fields — including `Id` and `IsAvailable` — bypassing the public constructor's defaults while keeping those fields read-only to external code.

```csharp
private sealed class Row
{
    public int Id { get; init; }
    public string MonthlyRent { get; init; } = "";
    public int IsAvailable { get; init; }
    // ...
    public RentalUnit ToUnit() => new(
        Id, Address, City, Province,
        decimal.Parse(MonthlyRent), Bedrooms,
        IsAvailable != 0,
        DateTime.Parse(LastUpdated, null, DateTimeStyles.RoundtripKind));
}
```

## Demo Scenarios

```
=== Maple Ridge Realty — Active Record Demo ===

1. Creating Rental Units      Save() four units; Id is assigned by the DB on each call
2. Querying Units             FindAvailable() and FindByCity() demonstrate static finders
3. Renting Units              Rent() enforces the "no double-rent" rule and auto-saves
4. Updating Rent & Vacating   UpdateRent() and Vacate() combine domain logic with persistence
5. Managing Tenants           Tenant active records with a FK to RentalUnits
6. Extending Lease & Cleanup  ExtendLease() persists; reload from DB verifies; Delete() removes rows
```

## When to Use

- You need a quick, self-contained data layer without the overhead of a separate repository or mapper.
- The domain is straightforward and closely mirrors the table structure (simple CRUD apps, admin panels, internal tools).
- You are working with a small team and want one place to find all logic for a given entity.
- The persistence technology is unlikely to change (you are not switching between SQL and NoSQL).
- You are building rapid prototypes or MVPs where iteration speed matters more than architectural purity.

## When NOT to Use

- The domain has complex invariants that span multiple objects — coordinating several Active Records in one transaction is awkward without a Unit of Work.
- You want to unit-test domain logic without a real database — Active Record ties tests to the database by design.
- You are applying Domain-Driven Design — DDD demands that domain objects are free of infrastructure concerns (use Data Mapper instead).
- The same entity must be persisted by multiple stores (SQL + a message bus, for example) — the object can only call one `Database.Connection`.
- The application is large and team-developed — scattering SQL across many record classes makes it hard to enforce consistent query patterns.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Simplicity | One class is one feature — no separate repository, mapper, or service needed for basic CRUD |
| Cohesion | The business rule ("can't rent twice") lives right next to the persistence code that enforces it |
| Discoverability | All operations on a `RentalUnit` are methods on `RentalUnit` — no hunting across layers |
| Low ceremony | Ideal for small domains where the overhead of Data Mapper or Repository adds no real value |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Testability | Domain logic cannot be tested without hitting a real database (or a test double for the connection) |
| Violates SRP | Each class carries two responsibilities: domain behaviour and persistence |
| Coupling | Changing the table schema forces changes to the domain class, and vice-versa |
| Transaction gaps | Coordinating two Active Records in one atomic transaction requires passing a connection externally, which undermines the self-contained design |
| Not DDD-friendly | Domain objects should be ignorant of persistence; Active Record does the opposite |

## Related Patterns

- **Data Mapper (4.07)** — the direct alternative: keeps the domain object completely free of database knowledge; preferred when the domain is complex or testability matters.
- **Repository (4.01)** — separates the query interface from the domain object; often used together with Data Mapper.
- **Unit of Work (4.02)** — solves the transaction problem Active Record leaves open; groups changes from multiple objects into one commit.
- **Domain Event (4.12)** — can be added to Active Record methods (e.g., `Rent()` could publish a `UnitRented` event after saving).

## Running the Demo

```bash
cd src/4-Enterprise/4.08-ActiveRecord/ActiveRecordPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.08-ActiveRecord/ActiveRecordPattern.Tests
dotnet test
```
