using SpecificationPattern;

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
    var list = products.ToList();
    if (list.Count == 0) { Console.WriteLine("  (none)"); return; }
    foreach (var p in list)
        Console.WriteLine($"  {p}");
    Console.WriteLine($"  → {list.Count} result(s)");
}

// ── Seed data ─────────────────────────────────────────────────────────────────
var products = new List<Product>
{
    new() { Id =  1, Name = "MacBook Air 15\"",           Category = "Electronics", Price = 1_699.99m, StockQuantity =  30, IsActive = true,  Rating = 4.8, Brand = "Apple"   },
    new() { Id =  2, Name = "Sony WH-1000XM5",            Category = "Electronics", Price =   399.99m, StockQuantity =  45, IsActive = true,  Rating = 4.7, Brand = "Sony"    },
    new() { Id =  3, Name = "Canon EOS R50",              Category = "Electronics", Price =   829.99m, StockQuantity =   6, IsActive = true,  Rating = 4.5, Brand = "Canon"   },
    new() { Id =  4, Name = "The North Face Jacket",      Category = "Clothing",    Price =   299.99m, StockQuantity =  80, IsActive = true,  Rating = 4.6, Brand = "TNF"     },
    new() { Id =  5, Name = "Levi's 501 Jeans",           Category = "Clothing",    Price =    98.99m, StockQuantity = 120, IsActive = true,  Rating = 4.3, Brand = "Levi's"  },
    new() { Id =  6, Name = "Instant Pot Duo 7-in-1",    Category = "Kitchen",     Price =   119.99m, StockQuantity =  60, IsActive = true,  Rating = 4.7, Brand = "Instant Pot" },
    new() { Id =  7, Name = "Fitbit Charge 6",            Category = "Fitness",     Price =   179.99m, StockQuantity =   0, IsActive = false, Rating = 4.1, Brand = "Fitbit"  },
    new() { Id =  8, Name = "Apple AirPods Pro 2",        Category = "Electronics", Price =   329.99m, StockQuantity =   8, IsActive = true,  Rating = 4.6, Brand = "Apple"   },
    new() { Id =  9, Name = "Arc'teryx Granville Pack",   Category = "Accessories", Price =   148.99m, StockQuantity =  25, IsActive = true,  Rating = 4.4, Brand = "Arc'teryx"},
    new() { Id = 10, Name = "Vitamix E310 Blender",       Category = "Kitchen",     Price =   449.99m, StockQuantity =   4, IsActive = true,  Rating = 4.9, Brand = "Vitamix" },
    new() { Id = 11, Name = "Canada Goose Expedition",    Category = "Clothing",    Price = 1_295.00m, StockQuantity =   0, IsActive = false, Rating = 4.8, Brand = "Canada Goose"},
    new() { Id = 12, Name = "Garmin Forerunner 265",      Category = "Fitness",     Price =   599.99m, StockQuantity =  15, IsActive = true,  Rating = 4.5, Brand = "Garmin"  },
};

var repo = new ProductRepository(products);

Console.WriteLine("=== Specification Pattern — Product Catalogue ===\n");

// ─── THE PROBLEM ──────────────────────────────────────────────────────────────
Header("THE PROBLEM — query logic scattered and duplicated");
Console.WriteLine("""

  Without Specification, every caller writes its own filter predicate:

    // In ProductService:
    var featured = products.Where(p => p.IsActive && p.StockQuantity > 0 && p.Rating >= 4.5);

    // In ReorderService:
    var reorder  = products.Where(p => p.IsActive && p.StockQuantity > 0 && p.StockQuantity <= 10);

    // In DiscountService:
    var eligible = products.Where(p => p.IsActive && p.Category == "Electronics" && p.Price > 500m);

  Problems:
  - The same rule ("is active", "is in stock") is copy-pasted everywhere
  - Business rules have no names — a lambda doesn't communicate intent
  - Combining rules requires nesting conditions that become hard to read
  - Repository.Find() must accept Func<T,bool> OR Expression<Func<T,bool>> —
    the leaky abstraction from 4.01 Repository

  Specification fixes this: each rule is a named, reusable, combinable object.

""");
Pause();

// ─── Build specs once ─────────────────────────────────────────────────────────
var active      = new ActiveSpecification();
var inStock     = new InStockSpecification();
var electronics = new CategorySpecification("Electronics");
var kitchen     = new CategorySpecification("Kitchen");
var highRating  = new MinRatingSpecification(4.5);
var premium     = new PriceRangeSpecification(500m, decimal.MaxValue);
var lowStock    = new LowStockSpecification(threshold: 10);

// ─── DEMO 1: Simple specs ─────────────────────────────────────────────────────
Header("DEMO 1 — Simple specifications");
Console.WriteLine();

Console.WriteLine("  Active products:");
PrintProducts(repo.Find(active));
Console.WriteLine();
Console.WriteLine("  In-stock products:");
PrintProducts(repo.Find(inStock));
Pause();

// ─── DEMO 2: And combinations ─────────────────────────────────────────────────
Header("DEMO 2 — And: combining specs");
Console.WriteLine();

Console.WriteLine("  Active AND in-stock AND Electronics:");
PrintProducts(repo.Find(active.And(inStock).And(electronics)));

Console.WriteLine();
Console.WriteLine("  Active AND in-stock AND Kitchen:");
PrintProducts(repo.Find(active.And(inStock).And(kitchen)));
Pause();

// ─── DEMO 3: Or and Not ───────────────────────────────────────────────────────
Header("DEMO 3 — Or / Not");
Console.WriteLine();

Console.WriteLine("  Electronics OR Fitness (active, in-stock):");
PrintProducts(repo.Find(active.And(inStock).And(electronics.Or(new CategorySpecification("Fitness")))));

Console.WriteLine();
Console.WriteLine("  Active AND NOT Electronics (all categories except Electronics):");
PrintProducts(repo.Find(active.And(electronics.Not())));
Pause();

// ─── DEMO 4: Business-rule specs ─────────────────────────────────────────────
Header("DEMO 4 — Named business rules as composed specs");
Console.WriteLine();

// "Featured" = active, in stock, rating >= 4.5
var featured = active.And(inStock).And(highRating);
Console.WriteLine("  Featured products (active + in-stock + rating ≥ 4.5):");
PrintProducts(repo.Find(featured));

Console.WriteLine();
// "Low stock alert" = active, stock 1–10
Console.WriteLine("  Low-stock alert (active, 1–10 units remaining):");
PrintProducts(repo.Find(active.And(lowStock)));

Console.WriteLine();
// "Premium Electronics" = active, electronics, price >= $500
var premiumElectronics = active.And(electronics).And(premium);
Console.WriteLine("  Premium Electronics (active + Electronics + price ≥ $500):");
PrintProducts(repo.Find(premiumElectronics));
Pause();

// ─── DEMO 5: IsSatisfiedBy on a single object ─────────────────────────────────
Header("DEMO 5 — IsSatisfiedBy: validate a single product");
Console.WriteLine();

var newProduct = new Product
{
    Id = 99, Name = "Dyson V15 Detect", Category = "Appliances",
    Price = 899.99m, StockQuantity = 20, IsActive = true, Rating = 4.6
};

Console.WriteLine($"  New product: {newProduct}\n");
Console.WriteLine($"  active.IsSatisfiedBy        → {active.IsSatisfiedBy(newProduct)}");
Console.WriteLine($"  inStock.IsSatisfiedBy       → {inStock.IsSatisfiedBy(newProduct)}");
Console.WriteLine($"  electronics.IsSatisfiedBy   → {electronics.IsSatisfiedBy(newProduct)}");
Console.WriteLine($"  highRating.IsSatisfiedBy    → {highRating.IsSatisfiedBy(newProduct)}");
Console.WriteLine($"  featured.IsSatisfiedBy      → {featured.IsSatisfiedBy(newProduct)}");

Console.WriteLine("""

  IsSatisfiedBy lets the same specification object be used as a
  guard in a service method, a validator in a domain event handler,
  or a filter in a repository query — without duplicating the rule.

""");
Pause();

// ─── DEMO 6: ToExpression — ORM-ready ────────────────────────────────────────
Header("DEMO 6 — ToExpression: the same rule as an expression tree");
Console.WriteLine();

Console.WriteLine("  featured.ToExpression() produces an Expression<Func<Product,bool>>.");
Console.WriteLine("  In an EF Core repository this becomes a SQL WHERE clause:\n");
Console.WriteLine("    _context.Products.Where(featured.ToExpression()).ToListAsync()");
Console.WriteLine();
Console.WriteLine("  SQL generated (approximately):");
Console.WriteLine("    SELECT * FROM Products");
Console.WriteLine("    WHERE IsActive = 1");
Console.WriteLine("      AND StockQuantity > 0");
Console.WriteLine("      AND Rating >= 4.5");
Console.WriteLine();
Console.WriteLine("  In-memory demo uses IsSatisfiedBy (compiled from ToExpression).");
Console.WriteLine("  EF Core uses ToExpression directly — same rule, zero duplication.");
Pause();

Console.WriteLine("  Done.");
