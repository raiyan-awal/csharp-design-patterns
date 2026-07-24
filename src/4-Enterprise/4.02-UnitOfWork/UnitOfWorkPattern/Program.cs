using Microsoft.Data.Sqlite;
using UnitOfWorkPattern;

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}

static void Header(string title)
{
    Console.WriteLine(new string('─', 62));
    Console.WriteLine($"  {title}");
    Console.WriteLine(new string('─', 62));
}

static void PrintProducts(IEnumerable<Product> products)
{
    foreach (var p in products)
        Console.WriteLine($"  {p}");
}

Console.WriteLine("=== Unit of Work Pattern — Order Placement ===\n");

// ─── THE PROBLEM ──────────────────────────────────────────────────────────────
Header("THE PROBLEM — repositories commit immediately, one at a time");
Console.WriteLine("""

  With plain repositories, every call writes straight through:

    var hoodie = await productRepo.GetByIdAsync(1);
    hoodie.StockQuantity -= 2;
    await productRepo.UpdateAsync(hoodie);      // ← committed immediately

    var pan = await productRepo.GetByIdAsync(3);
    if (pan.StockQuantity < 10)
        throw new InvalidOperationException("Insufficient stock");  // ← too late!

  The hoodie's stock was already decremented and persisted before the pan
  check failed. There is no order, no rollback, and inventory is now wrong —
  two hoodies vanished from stock with no order to explain where they went.

  Unit of Work fixes this: every repository involved in the business
  transaction stages its changes, and NONE of them become durable until a
  single CommitAsync call — either everything is written, or nothing is.

""");
Pause();

// ─── SETUP ────────────────────────────────────────────────────────────────────
var store = InMemoryDataStore.SeedCanadian();
Console.WriteLine("  Seeded catalogue:");
PrintProducts(store.Products);
Pause();

// ─── DEMO 1: successful order — one Commit, two repositories ─────────────────
Header("DEMO 1 — successful order: Products + Orders committed as one unit");
Console.WriteLine();

using (IUnitOfWork uow = new InMemoryUnitOfWork(store))
{
    var order = await OrderService.PlaceOrderAsync(uow, "Priya Sharma",
    [
        new CartLine(ProductId: 1, Quantity: 2),   // Roots Cabin Hoodie
        new CartLine(ProductId: 2, Quantity: 1),   // Canada Goose Toque
    ]);

    Console.WriteLine($"\n  {order}");
    foreach (var item in order.Items)
        Console.WriteLine($"    {item}");
}

Console.WriteLine("\n  Store after commit:");
PrintProducts(store.Products);
Pause();

// ─── DEMO 2: insufficient stock — nothing is written ─────────────────────────
Header("DEMO 2 — insufficient stock: the whole transaction is discarded");
Console.WriteLine();

Console.WriteLine("  Before attempt:");
PrintProducts(store.Products);
Console.WriteLine();

using (IUnitOfWork uow = new InMemoryUnitOfWork(store))
{
    try
    {
        // Hoodie (Id 1) succeeds first; Muskoka Pan (Id 3) only has 3 in stock,
        // so requesting 10 throws before either line is committed.
        await OrderService.PlaceOrderAsync(uow, "Jordan Lee",
        [
            new CartLine(ProductId: 1, Quantity: 1),
            new CartLine(ProductId: 3, Quantity: 10),
        ]);
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"  Order rejected: {ex.Message}");
    }
}

Console.WriteLine("\n  After the failed attempt — hoodie stock is UNCHANGED:");
PrintProducts(store.Products);
Console.WriteLine($"\n  Total orders in store: {store.Orders.Count} (still just the one from Demo 1)");
Pause();

// ─── DEMO 3: explicit RollbackAsync ───────────────────────────────────────────
Header("DEMO 3 — explicit RollbackAsync: change your mind before Commit");
Console.WriteLine();

using (IUnitOfWork uow = new InMemoryUnitOfWork(store))
{
    var toque = await uow.Products.GetByIdAsync(2);
    toque!.StockQuantity = 0;
    await uow.Products.UpdateAsync(toque);
    Console.WriteLine("  Staged: set Canada Goose Toque stock to 0 (not committed yet)");

    await uow.RollbackAsync();
}

var toqueAfter = store.Products.Single(p => p.Id == 2);
Console.WriteLine($"  Toque stock after rollback: {toqueAfter.StockQuantity} (unchanged)");
Pause();

// ─── DEMO 4: SQL Unit of Work — real IDbTransaction ──────────────────────────
Header("DEMO 4 — SqlUnitOfWork: the same OrderService, a real SQLite transaction");
Console.WriteLine("""

  OrderService.PlaceOrderAsync doesn't change at all — only the IUnitOfWork
  implementation passed to it changes. This time every Products.UpdateAsync
  and Orders.AddAsync call runs inside one real ADO.NET transaction.

""");

using var sqlConn = new SqliteConnection("Data Source=:memory:");
SqlUnitOfWork.SeedCanadian(sqlConn);

using (IUnitOfWork sqlUow = new SqlUnitOfWork(sqlConn))
{
    var order = await OrderService.PlaceOrderAsync(sqlUow, "Amara Okafor",
    [
        new CartLine(ProductId: 4, Quantity: 1),   // Blundstone 550 Boots
    ]);
    Console.WriteLine($"  {order}");
    foreach (var item in order.Items)
        Console.WriteLine($"    {item}");
}

using (IUnitOfWork verifyUow = new SqlUnitOfWork(sqlConn))
{
    var boots = await verifyUow.Products.GetByIdAsync(4);
    Console.WriteLine($"\n  Re-read via a fresh SqlUnitOfWork on the same connection: {boots}");
}
Pause();

// ─── DEMO 5: SQL failure — transaction rolled back by the database itself ───
Header("DEMO 5 — SQL failure: the database rolls back, not application code");
Console.WriteLine();

using (IUnitOfWork sqlUow = new SqlUnitOfWork(sqlConn))
{
    try
    {
        await OrderService.PlaceOrderAsync(sqlUow, "Marc Tremblay",
        [
            new CartLine(ProductId: 4, Quantity: 100),   // only ~9 left — fails
        ]);
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"  Order rejected: {ex.Message}");
        await sqlUow.RollbackAsync();
    }
}

using (IUnitOfWork verifyUow = new SqlUnitOfWork(sqlConn))
{
    var boots = await verifyUow.Products.GetByIdAsync(4);
    Console.WriteLine($"  Boots stock after the failed order: {boots} (unchanged since Demo 4)");
}
Pause();

// ─── DEMO 6: swapping implementations ────────────────────────────────────────
Header("DEMO 6 — OrderService never changes; only the IUnitOfWork does");
Console.WriteLine("""

    IUnitOfWork uow = new InMemoryUnitOfWork(store);          // tests & demo
    IUnitOfWork uow = new SqlUnitOfWork(sqlConnection);        // production SQL

    await OrderService.PlaceOrderAsync(uow, customerName, cart);

  Same call, same business rules, same all-or-nothing guarantee — only the
  storage technology underneath changed.

""");
Pause();

Console.WriteLine("  Done.");
