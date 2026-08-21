# 4.10 — Lazy Load

## Intent

Lazy Load defers the initialization of an expensive or optional part of an object until it is actually needed. Instead of populating every association when the parent object is loaded, the related data is fetched on demand — the first time it is accessed. The goal is to avoid work (and database round-trips) that never gets used.

## The Problem It Solves

```csharp
// Eagerly loading every company and all of its employees every time
public IReadOnlyList<Company> FindAll()
{
    var companies = _db.Query<CompanyRow>("SELECT * FROM Companies").ToList();
    foreach (var company in companies)
        company.Employees = LoadEmployees(company.Id);   // N+1 queries, every call
    return companies;
}
```

Problems with this approach:

- **N+1 queries** — one query for the parent table, then one more per row, even when the caller never uses the related data.
- **Memory waste** — all associations live in memory for the entire request, whether they are read or not.
- **Slow cold paths** — a list endpoint that shows only company names triggers a full employee load for every row.
- **Brittle design** — deep object graphs magnify the cost; a company that also eagerly loads projects, tasks, and comments turns one page request into hundreds of queries.

## Solution: Defer the Load Until First Access

The association property's getter triggers the database call the first time it is read. Subsequent reads return the already-loaded value with no second query.

```csharp
// Caller's view is identical regardless of variant
var companies = repo.FindAll();         // no employee queries fired
Console.WriteLine(companies[0].Name);  // still no employee query

var employees = companies[0].Employees; // ← ONE query fires here, on demand
Console.WriteLine(employees.Count);    // cached; no second query
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Domain interface | `ICompany` | Contract shared by all three variants |
| Lazy Initialization | `Company` | Holds a `Func<>` loader; null-coalesces on first access |
| Value Holder | `LazyTCompany` | Wraps loader in `System.Lazy<T>`; thread-safe by default |
| Virtual Proxy | `CompanyProxy` | Holds only the Id; loads the real `ICompany` on first non-Id access |
| Repository | `CompanyRepository` | Produces all three variants; encapsulates `LoadEmployees` |
| Schema | `Schema` | Creates the SQLite tables |

## Structure

```
4.10-LazyLoad/
├── LazyLoadPattern/
│   ├── Domain/
│   │   ├── ICompany.cs          ← shared interface (EmployeesLoaded + Employees)
│   │   ├── Company.cs           ← Lazy Initialization via ??= and Func<>
│   │   ├── LazyTCompany.cs      ← Value Holder via System.Lazy<T>
│   │   └── Employee.cs          ← plain record (Id, CompanyId, Name, Role, Salary)
│   ├── Proxies/
│   │   └── CompanyProxy.cs      ← Virtual Proxy; IsLoaded tells callers whether real object exists
│   ├── Infrastructure/
│   │   ├── Schema.cs            ← CREATE TABLE for Companies + Employees
│   │   └── CompanyRepository.cs ← Insert, FindById, FindAll, FindByIdLazyT, FindAllLazyT, Proxy
│   └── Program.cs               ← 5-section demo: seed, Lazy Init, Lazy<T>, Proxy, independent loading
└── LazyLoadPattern.Tests/
    └── LazyLoadTests.cs         ← 19 tests: unit (pure Func) + integration (SQLite)
```

## Key Code

### Lazy Initialization — `??=` with a captured `Func<>`

```csharp
public sealed class Company : ICompany
{
    private readonly Func<IReadOnlyList<Employee>> _loadEmployees;
    private IReadOnlyList<Employee>? _employees;

    public bool EmployeesLoaded => _employees is not null;

    public IReadOnlyList<Employee> Employees
    {
        get
        {
            _employees ??= _loadEmployees();
            return _employees;
        }
    }
}
```

The `Func<>` is injected by the repository at construction time. The first `Employees` access evaluates the delegate; all subsequent accesses return the cached field. Simple, zero-dependency, single-threaded safe.

### Value Holder — `System.Lazy<T>`

```csharp
public sealed class LazyTCompany : ICompany
{
    private readonly Lazy<IReadOnlyList<Employee>> _employees;

    public bool EmployeesLoaded => _employees.IsValueCreated;
    public IReadOnlyList<Employee> Employees => _employees.Value;

    public LazyTCompany(int id, string name, string industry, string city,
                        Func<IReadOnlyList<Employee>> loadEmployees)
    {
        _employees = new Lazy<IReadOnlyList<Employee>>(loadEmployees);
        // default mode: LazyThreadSafetyMode.ExecutionAndPublication
    }
}
```

`System.Lazy<T>` is the idiomatic .NET choice when thread-safety matters. It guarantees the factory runs at most once, even under concurrent access. `IsValueCreated` surfaces the loaded state without triggering the load.

### Virtual Proxy — stand-in that loads on first non-Id access

```csharp
public sealed class CompanyProxy : ICompany
{
    private readonly Func<int, ICompany> _loader;
    private ICompany? _real;

    public int Id { get; }
    public bool IsLoaded => _real is not null;

    private ICompany Real => _real ??= _loader(Id);

    public string Name     => Real.Name;
    public string Industry => Real.Industry;
    public string City     => Real.City;
    public bool   EmployeesLoaded => _real is not null && _real.EmployeesLoaded;
    public IReadOnlyList<Employee> Employees => Real.Employees;
}
```

`Id` is returned directly — the proxy knows its own identity without consulting the database. Every other property delegates through `Real`, which triggers the load on first call. `IsLoaded` lets callers inspect load state without causing it.

### Repository wiring

```csharp
public Company? FindById(int id)
{
    var row = _db.QuerySingleOrDefault<CompanyRow>("SELECT * FROM Companies WHERE Id = @id", new { id });
    return row is null ? null : new Company(row.Id, row.Name, row.Industry, row.City, () => LoadEmployees(row.Id));
}

public CompanyProxy Proxy(int id) =>
    new(id, proxyId => FindById(proxyId) ?? throw new KeyNotFoundException($"Company {proxyId} not found."));

private IReadOnlyList<Employee> LoadEmployees(int companyId) =>
    _db.Query<EmployeeRow>("SELECT * FROM Employees WHERE CompanyId = @companyId ORDER BY Name", new { companyId })
       .Select(r => new Employee(r.Id, r.CompanyId, r.Name, r.Role, decimal.Parse(r.Salary)))
       .ToList();
```

The loader closure captures `row.Id`, so each `Company` knows which employees to fetch without a back-reference to the repository being stored on the domain object itself.

## Demo Scenarios

```
=== Maple Leaf Technologies — Lazy Load Demo ===

--- Seeding the Directory ---
4 companies (Shopify/Ottawa, RBC Royal Bank/Toronto, Suncor Energy/Calgary,
Bombardier/Montreal) and 12 employees (3 per company) seeded.

--- Variant 1: Lazy Initialization ---
Loaded 4 companies — no employee queries fired yet.
Access Shopify.Employees → ONE query fires; RBC still unloaded.
Second access → same list instance (no second query).

--- Variant 2: System.Lazy<T> ---
IsValueCreated false before access, true after.
RBC team loaded and printed; other companies untouched.

--- Variant 3: Virtual Proxy ---
4 proxies created; none loaded yet.
Access Suncor proxy Name → real Company loaded; 3 proxies still unloaded.

--- Independent Loading ---
Load Bombardier employees; confirm Shopify, RBC, Suncor still unloaded.
```

## When to Use

- You have associations that are expensive to load (large collections, remote calls) and are not always needed.
- Multiple call sites fetch the parent object but only some of them need the related data.
- You want to avoid the N+1 query problem without switching to a different ORM or adding manual joins everywhere.
- You are working with a domain model where the persistence layer is separate from the domain (Data Mapper, Repository, Service Layer).

## When NOT to Use

- The related data is always needed — eager loading in a single JOIN is cheaper than a second round-trip.
- Your application is heavily multi-threaded and you choose Lazy Initialization (not `System.Lazy<T>`) — the `??=` variant is not thread-safe.
- The entity is small and lives entirely in memory (no database) — lazy loading adds complexity with no benefit.
- You use an ORM that already provides lazy loading (EF Core's virtual navigation properties with a proxy provider) — hand-rolling it duplicates framework work.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Fewer queries | Related data is only fetched when actually read, eliminating unnecessary round-trips. |
| Lower memory pressure | Objects that are never accessed never consume heap space for their associations. |
| Faster initial loads | A list of 100 companies loads in one query instead of 101. |
| Transparent to callers | All three variants expose the same `ICompany` interface; callers do not need to know which variant they hold. |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Hidden queries | Accessing a navigation property inside a loop can silently cause N+1 queries if the caller is not aware of the pattern. |
| Session coupling | The loader closure keeps an implicit reference to the database connection; once the connection is closed, a late access throws. |
| Complexity overhead | Three classes instead of one — more code to read, test, and maintain than a simple eager load. |
| Thread-safety gap | Lazy Initialization (`??=`) is not safe under concurrent access; `System.Lazy<T>` is required for shared instances. |

## Related Patterns

- **Identity Map (4.09)** — pairs naturally with Lazy Load; the map ensures that a lazy-loaded association returns the same object instance that may already be in the map, preventing duplicate loads.
- **Data Mapper (4.07)** — the repository pattern shown here is a direct application of Data Mapper; the loader closure is the point where the mapper and the lazy load cooperate.
- **Proxy (2.7)** — Virtual Proxy is the Structural Proxy pattern applied to deferred loading; the intent is identical, the trigger is a first property access rather than an access-control check.
- **Repository (4.01)** — `CompanyRepository` is the Repository that produces lazily-loaded domain objects, keeping the loading strategy out of the domain layer.

## Running the Demo

```bash
cd src/4-Enterprise/4.10-LazyLoad/LazyLoadPattern && dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.10-LazyLoad/LazyLoadPattern.Tests && dotnet test
```
