# 4.02 — Unit of Work Pattern

## Intent

Maintain a list of objects affected by a business transaction and coordinate writing out all of their changes as a single, all-or-nothing operation. Where a repository commits each call immediately, a Unit of Work defers every write until one explicit `CommitAsync` — so a business transaction spanning multiple repositories either succeeds completely or leaves no trace.

---

## The Problem It Solves

A repository, on its own, has no concept of a business transaction — every `UpdateAsync` or `AddAsync` writes through immediately:

```csharp
// WITHOUT Unit of Work — each repository call commits on its own:
var hoodie = await productRepo.GetByIdAsync(1);
hoodie.StockQuantity -= 2;
await productRepo.UpdateAsync(hoodie);      // committed immediately

var pan = await productRepo.GetByIdAsync(3);
if (pan.StockQuantity < 10)
    throw new InvalidOperationException("Insufficient stock");   // too late
```

The hoodie's stock was already decremented and persisted before the pan's stock check failed. There is no order, no rollback — two hoodies vanished from inventory with nothing to explain where they went. Multiple repositories (or multiple calls to the same repository) need to succeed or fail **together**.

---

## Solution: Order Placement Across Two Repositories

`OrderService.PlaceOrderAsync` decrements `Product` stock and inserts an `Order` — two repositories, one business transaction. It depends only on `IUnitOfWork`; every `Products.UpdateAsync` and `Orders.AddAsync` call **stages** a change, and nothing becomes durable until `uow.CommitAsync()` runs.

### Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| **Unit of Work interface** | `IUnitOfWork` | Exposes the repositories for one transaction + `CommitAsync`/`RollbackAsync` |
| **In-memory impl** | `InMemoryUnitOfWork` | Stages changes in a dictionary/list; applies them to a shared store on Commit |
| **SQL impl** | `SqlUnitOfWork` | Opens one `IDbTransaction`; every repository call enlists in it |
| **Repositories** | `IProductRepository`, `IOrderRepository` | Minimal read/write contracts scoped to what the transaction needs |
| **Business logic** | `OrderService` | Coordinates both repositories, then calls `CommitAsync` exactly once |

---

## Structure

```
UnitOfWorkPattern/
├── Product.cs                  ← entity with Clone() — GetByIdAsync never returns a live reference
├── Order.cs / OrderItem.cs     ← order aggregate
├── IProductRepository.cs       ← GetByIdAsync, UpdateAsync
├── IOrderRepository.cs         ← GetByIdAsync, AddAsync
├── IUnitOfWork.cs               ← Products, Orders, CommitAsync, RollbackAsync
├── InMemoryDataStore.cs        ← stands in for "the database"
├── InMemoryUnitOfWork.cs       ← stages writes; nested StagedProductRepository/StagedOrderRepository
├── SqlProductRepository.cs     ← Dapper, every call takes the shared IDbTransaction
├── SqlOrderRepository.cs       ← same, plus OrderItems insert
├── SqlUnitOfWork.cs            ← opens/commits/rolls back one IDbTransaction
└── OrderService.cs             ← business logic; depends only on IUnitOfWork
```

---

## Key Code

### The interface — what business logic depends on

```csharp
public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    IOrderRepository   Orders   { get; }

    Task CommitAsync();
    Task RollbackAsync();
}
```

### Business logic — one Commit, no matter how many repositories were touched

```csharp
public static async Task<Order> PlaceOrderAsync(IUnitOfWork uow, string customerName, IEnumerable<CartLine> cart)
{
    var order = new Order { CustomerName = customerName, OrderDate = DateTime.UtcNow };

    foreach (var line in cart)
    {
        var product = await uow.Products.GetByIdAsync(line.ProductId)
            ?? throw new InvalidOperationException($"Product #{line.ProductId} not found");

        if (product.StockQuantity < line.Quantity)
            throw new InvalidOperationException($"Insufficient stock for {product.Name}");

        product.StockQuantity -= line.Quantity;
        await uow.Products.UpdateAsync(product);          // staged, not written yet

        order.Items.Add(new OrderItem { ProductId = product.Id, ProductName = product.Name,
                                         Quantity = line.Quantity, UnitPrice = product.Price });
    }

    order.TotalAmount = order.Items.Sum(i => i.LineTotal);
    await uow.Orders.AddAsync(order);                       // staged, not written yet

    await uow.CommitAsync();                                 // now — and only now — everything is durable
    return order;
}
```

If the stock check throws partway through the loop, `CommitAsync` is never reached — every `UpdateAsync` call made so far stays staged and is simply discarded.

### In-memory staging — the store is only touched inside Commit

```csharp
public Task CommitAsync()
{
    lock (_store.Gate)
    {
        foreach (var staged in _stagedProducts.Values)
        {
            var existing = _store.Products.First(p => p.Id == staged.Id);
            existing.StockQuantity = staged.StockQuantity;   // applied here, not in UpdateAsync
        }

        foreach (var order in _stagedOrders)
        {
            order.Id = _store.NextOrderId++;
            _store.Orders.Add(order);
        }
    }
    _stagedProducts.Clear();
    _stagedOrders.Clear();
    return Task.CompletedTask;
}
```

### SQL implementation — the database enforces the same guarantee natively

```csharp
public SqlUnitOfWork(IDbConnection connection)
{
    _conn = connection;
    _tx = _conn.BeginTransaction();
    Products = new SqlProductRepository(_conn, _tx);   // every query passes _tx
    Orders   = new SqlOrderRepository(_conn, _tx);
}

public Task CommitAsync()   { _tx.Commit();   return Task.CompletedTask; }
public Task RollbackAsync() { _tx.Rollback(); return Task.CompletedTask; }

public void Dispose()
{
    if (!_completed) _tx.Rollback();   // Commit was never called — roll back automatically
    _tx.Dispose();                      // connection is NOT owned/disposed here — the caller manages it
}
```

`OrderService.PlaceOrderAsync` runs completely unchanged against either implementation — only the `IUnitOfWork` passed in changes.

---

## Why `GetByIdAsync` Returns a Clone

`InMemoryUnitOfWork.Products.GetByIdAsync` returns `product.Clone()`, never the live object sitting in `InMemoryDataStore`. If it returned the live reference, `product.StockQuantity -= line.Quantity` in `OrderService` would mutate the "committed" store immediately — defeating the entire point of staging. The clone is mutated freely by the caller; only `UpdateAsync` (which clones again into the staging dictionary) and then `CommitAsync` (which copies staged fields onto the real store entry) ever touch the store.

---

## Repository vs Unit of Work

| | Repository alone | Repository + Unit of Work |
|---|---|---|
| When does a write become durable? | Immediately, on each call | Only when `CommitAsync` runs |
| Multiple repositories in one operation | Each commits independently — partial failure leaves inconsistent state | All commit together or not at all |
| Who decides the transaction boundary? | Nobody — every call is its own boundary | The business logic (`OrderService`), explicitly |

---

## Demo Scenarios

```
PROBLEM  — plain repositories commit immediately; a mid-transaction failure leaves inventory wrong
SETUP    — seed an InMemoryDataStore with four Canadian products
DEMO 1   — successful order: two Products.UpdateAsync + one Orders.AddAsync, one CommitAsync
DEMO 2   — insufficient stock: exception thrown before Commit — store is completely untouched
DEMO 3   — explicit RollbackAsync: stage a change, then discard it deliberately
DEMO 4   — SqlUnitOfWork: identical OrderService call, backed by a real SQLite IDbTransaction
DEMO 5   — SQL failure: the database itself rolls back the transaction, not application code
DEMO 6   — OrderService is unchanged; only the IUnitOfWork implementation passed to it varies
```

---

## When to Use

- A single business operation must update more than one aggregate/repository atomically
- You're coordinating writes across repositories and need an explicit transaction boundary
- You want the option to swap an in-memory transaction (tests) for a real database transaction (production) without touching business logic

## When NOT to Use

- Every write is already a single, independent repository call — there is no multi-step transaction to coordinate
- Your ORM's `DbContext`/`SaveChangesAsync` (EF Core) already implements Unit of Work — wrapping it again adds nothing
- The operation only ever touches one row in one table

---

## Benefits

| Benefit | Explanation |
|---------|-------------|
| **Atomicity** | Every repository call in a transaction succeeds or fails together |
| **Explicit transaction boundary** | `CommitAsync` marks exactly where "this business operation" ends |
| **Swappable transaction technology** | In-memory staging for tests, `IDbTransaction` for production — business logic is identical |
| **Consistent reads mid-transaction** | A repository can return its own staged-but-uncommitted values to a later call within the same Unit of Work |

### Example — reading your own uncommitted write

```csharp
using var uow = new InMemoryUnitOfWork(store);

var product = await uow.Products.GetByIdAsync(1);
product!.StockQuantity -= 5;
await uow.Products.UpdateAsync(product);          // staged only — store is untouched

var sameProduct = await uow.Products.GetByIdAsync(1);
// sameProduct.StockQuantity already reflects the -5: StagedProductRepository
// checks its staged dictionary before falling back to the store. A second,
// independent `new InMemoryUnitOfWork(store)` would NOT see this change —
// only calls made through this same `uow` do, until CommitAsync applies it.
```

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| **Extra indirection** | Another interface between business logic and the repositories it already depends on |
| **Long-lived transactions** | Holding an `IDbTransaction` open across slow operations risks lock contention |
| **Redundant with EF Core** | `DbContext.SaveChangesAsync` already is a Unit of Work — implementing your own on top of EF Core duplicates it |

---

## Related Patterns

- **Repository (4.01)** — Unit of Work coordinates the repositories it exposes; neither pattern requires the other, but they're commonly paired
- **CQRS (4.03)** — write-side commands often execute inside a Unit of Work; the read side bypasses it entirely
- **Saga Pattern (4.19)** — coordinates a business transaction across *multiple services*, where a single local `IDbTransaction` isn't possible

---

## Running the Demo

```bash
cd src/4-Enterprise/4.02-UnitOfWork/UnitOfWorkPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.02-UnitOfWork/UnitOfWorkPattern.Tests
dotnet test
```
