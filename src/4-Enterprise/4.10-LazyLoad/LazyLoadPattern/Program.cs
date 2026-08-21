using Microsoft.Data.Sqlite;
using LazyLoadPattern.Domain;
using LazyLoadPattern.Infrastructure;
using LazyLoadPattern.Proxies;

using var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();
Schema.Create(connection);
var repo = new CompanyRepository(connection);

// ── Section 1: Seed the directory ─────────────────────────────────────────────
Console.WriteLine("=== Maple Leaf Technologies — Lazy Load Demo ===\n");
Console.WriteLine("--- Seeding the Directory ---");

var shopify    = repo.Insert("Shopify",        "E-Commerce",    "Ottawa");
var rbc        = repo.Insert("RBC Royal Bank", "Finance",       "Toronto");
var suncor     = repo.Insert("Suncor Energy",  "Energy",        "Calgary");
var bombardier = repo.Insert("Bombardier",     "Manufacturing", "Montreal");

repo.InsertEmployee("Liam Tremblay",   "Backend Engineer",     115_000m, shopify.Id);
repo.InsertEmployee("Sophie Chen",     "Staff Engineer",       145_000m, shopify.Id);
repo.InsertEmployee("Aiden Kowalski",  "DevOps",               105_000m, shopify.Id);
repo.InsertEmployee("Emily Park",      "Senior Analyst",       120_000m, rbc.Id);
repo.InsertEmployee("Noah Bouchard",   "Risk Manager",         135_000m, rbc.Id);
repo.InsertEmployee("Olivia Martin",   "Quantitative Analyst", 155_000m, rbc.Id);
repo.InsertEmployee("James Liu",       "Petroleum Engineer",   140_000m, suncor.Id);
repo.InsertEmployee("Chloe Patel",     "Environmental Lead",   125_000m, suncor.Id);
repo.InsertEmployee("Ethan Nguyen",    "Safety Officer",       110_000m, suncor.Id);
repo.InsertEmployee("Ava Singh",       "Aerospace Engineer",   130_000m, bombardier.Id);
repo.InsertEmployee("Mason Roy",       "Systems Integrator",   118_000m, bombardier.Id);
repo.InsertEmployee("Isabella Lavoie", "Quality Engineer",     112_000m, bombardier.Id);

Console.WriteLine("4 companies and 12 employees seeded.\n");
Console.WriteLine("Companies in directory:");
foreach (var c in repo.FindAll())
    Console.WriteLine($"  [{c.Id}] {c.Name} ({c.City}) — employees loaded? {c.EmployeesLoaded}");

Pause();

// ── Section 2: Lazy Initialization variant ───────────────────────────────────
Console.WriteLine("--- Variant 1: Lazy Initialization ---");
Console.WriteLine("Loading all companies — no employee queries fired yet.");

var companies = repo.FindAll();
Console.WriteLine($"Loaded {companies.Count} companies.");
Console.WriteLine($"Any employees loaded? {companies.Any(c => c.EmployeesLoaded)}\n");

var shopifyCompany = companies.First(c => c.Name == "Shopify");
Console.WriteLine("Accessing Shopify employees...");
var shopifyEmployees = shopifyCompany.Employees;  // triggers ONE employee query
Console.WriteLine($"  Shopify employees: {shopifyEmployees.Count}");
Console.WriteLine($"  shopify.EmployeesLoaded: {shopifyCompany.EmployeesLoaded}");
Console.WriteLine($"  RBC employees loaded:    {companies.First(c => c.Name == "RBC Royal Bank").EmployeesLoaded}");

Console.WriteLine("\nAccessing Shopify employees again (no second DB query):");
var again = shopifyCompany.Employees;
Console.WriteLine($"  Same list instance? {ReferenceEquals(shopifyEmployees, again)}");

Pause();

// ── Section 3: System.Lazy<T> variant ────────────────────────────────────────
Console.WriteLine("--- Variant 2: System.Lazy<T> ---");
Console.WriteLine("Same behaviour — thread-safe by default (LazyThreadSafetyMode.ExecutionAndPublication).\n");

var lazyTCompanies = repo.FindAllLazyT();
Console.WriteLine($"Loaded {lazyTCompanies.Count} companies (no employee queries).");

var rbcLazy = lazyTCompanies.First(c => c.Name == "RBC Royal Bank");
Console.WriteLine($"RBC IsValueCreated before access: {rbcLazy.EmployeesLoaded}");
var rbcEmployees = rbcLazy.Employees;
Console.WriteLine($"RBC IsValueCreated after access:  {rbcLazy.EmployeesLoaded}");
Console.WriteLine($"RBC team: {string.Join(", ", rbcEmployees.Select(e => e.Name))}");

Pause();

// ── Section 4: Virtual Proxy variant ─────────────────────────────────────────
Console.WriteLine("--- Variant 3: Virtual Proxy ---");
Console.WriteLine("Proxy holds only the Id — real Company loads on first property access.\n");

var proxies = new List<CompanyProxy>
{
    repo.Proxy(shopify.Id),
    repo.Proxy(rbc.Id),
    repo.Proxy(suncor.Id),
    repo.Proxy(bombardier.Id)
};

Console.WriteLine($"Created {proxies.Count} proxies — none loaded yet.");
Console.WriteLine($"Any real Company loaded? {proxies.Any(p => p.IsLoaded)}\n");

var suncorProxy = proxies.First(p => p.Id == suncor.Id);
Console.WriteLine($"Accessing Suncor proxy (Id={suncor.Id})...");
Console.WriteLine($"  Name:     {suncorProxy.Name}");        // triggers load
Console.WriteLine($"  IsLoaded: {suncorProxy.IsLoaded}");
Console.WriteLine($"  Employees: {suncorProxy.Employees.Count}");
Console.WriteLine($"\nOther proxies still unloaded: {proxies.Count(p => !p.IsLoaded)}");

Pause();

// ── Section 5: Independent loading ───────────────────────────────────────────
Console.WriteLine("--- Independent Loading ---");
Console.WriteLine("Accessing one company's employees does not load any other company's employees.\n");

var fresh = repo.FindAll();
var bom = fresh.First(c => c.Name == "Bombardier");
Console.WriteLine($"Bombardier employees: {bom.Employees.Count}");
Console.WriteLine($"  Shopify loaded?      {fresh.First(c => c.Name == "Shopify").EmployeesLoaded}");
Console.WriteLine($"  Suncor loaded?       {fresh.First(c => c.Name == "Suncor Energy").EmployeesLoaded}");
Console.WriteLine($"  RBC loaded?          {fresh.First(c => c.Name == "RBC Royal Bank").EmployeesLoaded}");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
