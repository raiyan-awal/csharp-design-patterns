using Microsoft.Data.Sqlite;
using RepositoryPattern;

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

static void PrintAll(IEnumerable<Product> products)
{
    foreach (var p in products)
        Console.WriteLine($"  {p}");
}

Console.WriteLine("=== Repository Pattern — Product Catalogue ===\n");

// ─── THE PROBLEM ──────────────────────────────────────────────────────────────
Header("THE PROBLEM — data access mixed into business logic");
Console.WriteLine("""

  Without Repository, data access leaks directly into service classes:

    public class ProductService
    {
        private readonly SqlConnection _db;

        public async Task<Product?> GetFeaturedProduct()
        {
            // Raw SQL in the middle of business logic:
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

  Repository moves all data access behind an interface. Business logic
  calls IRepository<Product> — it has no idea whether there is a SQL
  database, a MongoDB collection, or an in-memory list behind it.

""");
Pause();

// ─── SETUP ────────────────────────────────────────────────────────────────────
// Declared as the interface, not the concrete type — this is the point.
// Swap InMemoryProductRepository for SqlProductRepository and nothing else changes.
IRepository<Product> repo = new InMemoryProductRepository(seedData: true);
Console.WriteLine();

// ─── DEMO 1: GetById + GetAll ─────────────────────────────────────────────────
Header("DEMO 1 — GetByIdAsync and GetAllAsync");
Console.WriteLine();

var single = await repo.GetByIdAsync(1);
Console.WriteLine($"  GetByIdAsync(1) → {single}");

var missing = await repo.GetByIdAsync(99);
Console.WriteLine($"  GetByIdAsync(99) → {missing?.ToString() ?? "null (not found)"}");

Console.WriteLine($"\n  Total products: {await repo.CountAsync()}\n");
Console.WriteLine("  All products:");
PrintAll(await repo.GetAllAsync());
Pause();

// ─── DEMO 2: AddAsync ─────────────────────────────────────────────────────────
Header("DEMO 2 — AddAsync: repository assigns the ID");
Console.WriteLine("\n  Adding two new products:\n");

var airPods = new Product { Name = "Apple AirPods Pro 2", Category = "Electronics", Price = 329.99m, StockQuantity = 75 };
var boots   = new Product { Name = "Sorel Winter Boots",  Category = "Clothing",    Price = 199.99m, StockQuantity = 40 };

await repo.AddAsync(airPods);
await repo.AddAsync(boots);

Console.WriteLine($"\n  AirPods were assigned Id = {airPods.Id}");
Console.WriteLine($"  Boots were assigned Id   = {boots.Id}");
Console.WriteLine($"  Total products now: {await repo.CountAsync()}");
Pause();

// ─── DEMO 3: UpdateAsync ──────────────────────────────────────────────────────
Header("DEMO 3 — UpdateAsync: modify and persist");
Console.WriteLine();

var toUpdate = await repo.GetByIdAsync(1);
if (toUpdate != null)
{
    Console.WriteLine($"  Before: {toUpdate}");
    toUpdate.Price         = 1_549.99m;   // price drop
    toUpdate.StockQuantity = 22;
    await repo.UpdateAsync(toUpdate);

    var after = await repo.GetByIdAsync(1);
    Console.WriteLine($"  After:  {after}");
}
Pause();

// ─── DEMO 4: DeleteAsync + ExistsAsync ────────────────────────────────────────
Header("DEMO 4 — DeleteAsync and ExistsAsync");
Console.WriteLine();

Console.WriteLine($"  ExistsAsync(7) before delete → {await repo.ExistsAsync(7)}");
await repo.DeleteAsync(7);   // Fitbit Charge 6 — inactive, no stock
Console.WriteLine($"  ExistsAsync(7) after delete  → {await repo.ExistsAsync(7)}");
Console.WriteLine($"  Total products now: {await repo.CountAsync()}");
Pause();

// ─── DEMO 5: FindAsync ────────────────────────────────────────────────────────
Header("DEMO 5 — FindAsync: query with a predicate");
Console.WriteLine();

var electronics = await repo.FindAsync(p => p.Category == "Electronics" && p.IsActive);
Console.WriteLine($"  Electronics (active): {electronics.Count()} items");
PrintAll(electronics);

Console.WriteLine();
var pricey = await repo.FindAsync(p => p.Price > 500m);
Console.WriteLine($"  Price > $500: {pricey.Count()} items");
PrintAll(pricey);

Console.WriteLine();
var lowStock = await repo.FindAsync(p => p.StockQuantity < 30 && p.IsActive);
Console.WriteLine($"  Low stock (< 30 units, active): {lowStock.Count()} items");
PrintAll(lowStock);
Pause();

// ─── DEMO 6: Power of abstraction ────────────────────────────────────────────
Header("DEMO 6 — The interface is the contract; the implementation is swappable");
Console.WriteLine("""

  The variable `repo` is declared as IRepository<Product>.
  Every line of demo code above works identically regardless of which
  implementation sits behind it:

    IRepository<Product> repo = new InMemoryProductRepository();   // tests & demo
    IRepository<Product> repo = new SqlProductRepository(conn);    // production SQL
    IRepository<Product> repo = new MongoProductRepository(db);    // MongoDB
    IRepository<Product> repo = new CachedProductRepository(repo); // read-through cache
    IRepository<Product> repo = new ApiProductRepository(http);    // external REST API

  None of the code that USES the repository changes.
  This is Dependency Inversion in practice.

""");
Pause();

// ─── DEMO 7: SQL repository — same interface, real SQL ────────────────────────
Header("DEMO 7 — SqlProductRepository: same interface, real SQL (SQLite in-memory)");
Console.WriteLine("""

  SqlProductRepository uses Dapper to run real SQL statements.
  The calling code below is IDENTICAL to Demos 1–5 — only the
  constructor changes:

    // SQL Server in production:
    IRepository<Product> repo = new SqlProductRepository(new SqlConnection("Server=...;"));

    // SQLite for this demo (no server required — in-memory database):
    IRepository<Product> repo = new SqlProductRepository(new SqliteConnection("Data Source=:memory:"));

""");

using SqlProductRepository sqlImpl = new(new SqliteConnection("Data Source=:memory:"));
IRepository<Product> sqlRepo = sqlImpl;

var laptop    = new Product { Name = "Dell XPS 15",         Category = "Electronics", Price = 1_299.99m, StockQuantity = 20 };
var backpack  = new Product { Name = "Arc'teryx Granville", Category = "Accessories", Price =   149.99m, StockQuantity = 35 };
var headset   = new Product { Name = "Jabra Evolve2 85",    Category = "Electronics", Price =   499.99m, StockQuantity = 10 };

Console.WriteLine("  Adding products via SQL INSERT:");
await sqlRepo.AddAsync(laptop);
await sqlRepo.AddAsync(backpack);
await sqlRepo.AddAsync(headset);

Console.WriteLine($"\n  CountAsync() → {await sqlRepo.CountAsync()}");

var found = await sqlRepo.GetByIdAsync(laptop.Id);
Console.WriteLine($"  GetByIdAsync({laptop.Id}) → {found}");

Console.WriteLine();
laptop.Price = 1_149.99m;
await sqlRepo.UpdateAsync(laptop);

Console.WriteLine();
var sqlElectronics = await sqlRepo.FindAsync(p => p.Category == "Electronics");
Console.WriteLine($"  FindAsync (Electronics): {sqlElectronics.Count()} item(s)");
PrintAll(sqlElectronics);

Console.WriteLine();
await sqlRepo.DeleteAsync(backpack.Id);
Console.WriteLine($"  After DeleteAsync({backpack.Id}) — CountAsync() → {await sqlRepo.CountAsync()}");

Console.WriteLine("""

  The variable is declared as IRepository<Product> — not SqlProductRepository.
  The InMemory demos above and this SQL demo are driven by identical code paths.

""");
Pause();

Console.WriteLine("  Done.");
