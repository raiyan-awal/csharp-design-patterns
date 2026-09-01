using CacheAsidePattern.Core;
using CacheAsidePattern.Data;
using CacheAsidePattern.Domain;
using CacheAsidePattern.Services;

Console.WriteLine("=== 4.25 Cache-Aside — Maple Reads Book Catalogue ===");
Console.WriteLine();

// ── Seed data ────────────────────────────────────────────────────────────────

var repo = new InMemoryBookRepository();
repo.Seed([
    new("b1", "The Jade Peony",                   "Wayson Choy",       "978-0-7710-2145-4", 19.99m, "Literary Fiction"),
    new("b2", "In the Skin of a Lion",             "Michael Ondaatje",  "978-0-7710-6872-4", 21.99m, "Literary Fiction"),
    new("b3", "The Stone Angel",                   "Margaret Laurence", "978-0-7710-4730-9", 18.99m, "Literary Fiction"),
    new("b4", "Alias Grace",                       "Margaret Atwood",   "978-0-7710-0802-8", 22.99m, "Historical Fiction"),
    new("b5", "The Apprenticeship of Duddy Kravitz","Mordecai Richler", "978-0-7710-7562-3", 17.99m, "Literary Fiction"),
    new("b6", "Cat's Eye",                         "Margaret Atwood",   "978-0-7710-0803-5", 20.99m, "Literary Fiction"),
]);

var bookCache = new InMemoryCache<string, Book>();
var listCache = new InMemoryCache<string, IReadOnlyList<Book>>();
var catalogue = new BookCatalogueService(repo, bookCache, listCache);

// ── Section 1: Cold cache — all misses ───────────────────────────────────────

Console.WriteLine("── 1. Cold Cache — First Lookups (All Misses) ──");
Console.WriteLine();

var before = repo.LoadCount;
var jade = catalogue.GetById("b1");
var lion = catalogue.GetById("b2");
Console.WriteLine($"  '{jade!.Title}' by {jade.Author}  — ${jade.PriceCAD:F2} CAD");
Console.WriteLine($"  '{lion!.Title}' by {lion.Author}  — ${lion.PriceCAD:F2} CAD");
Console.WriteLine($"  DB queries: {repo.LoadCount - before}   Cache: {bookCache.Hits} hits / {bookCache.Misses} misses");

Pause();

// ── Section 2: Warm cache — repeated lookups skip the DB ─────────────────────

Console.WriteLine("── 2. Warm Cache — Repeated Lookups (All Hits) ──");
Console.WriteLine();

var hitsBefore = bookCache.Hits;
var dbBefore   = repo.LoadCount;
_ = catalogue.GetById("b1");
_ = catalogue.GetById("b1");
_ = catalogue.GetById("b2");
Console.WriteLine($"  Fetched b1 twice and b2 once — all served from cache");
Console.WriteLine($"  New DB queries: {repo.LoadCount - dbBefore}   New cache hits: {bookCache.Hits - hitsBefore}");

Pause();

// ── Section 3: Author list caching ───────────────────────────────────────────

Console.WriteLine("── 3. Author List Caching ──");
Console.WriteLine();

var atwood1 = catalogue.GetByAuthor("Margaret Atwood");
Console.WriteLine($"  First call  — fetched {atwood1.Count} Atwood books from DB");

var atwood2 = catalogue.GetByAuthor("Margaret Atwood");
Console.WriteLine($"  Second call — fetched {atwood2.Count} Atwood books from cache");
Console.WriteLine($"  List cache: {listCache.Hits} hits / {listCache.Misses} misses");

Pause();

// ── Section 4: Write invalidation ────────────────────────────────────────────

Console.WriteLine("── 4. Write Invalidation — Save Clears Stale Cache ──");
Console.WriteLine();

// b1 is currently cached at $19.99
Console.WriteLine($"  Before save — cached price: ${catalogue.GetById("b1")!.PriceCAD:F2}");

// update the price in the repository; service invalidates cache entry
var updatedJade = jade with { PriceCAD = 24.99m };
catalogue.Save(updatedJade);

var dbLoad = repo.LoadCount;
var fresh = catalogue.GetById("b1");   // cache miss → reloaded from repo
Console.WriteLine($"  After save  — fresh price : ${fresh!.PriceCAD:F2}  (DB reloaded: {repo.LoadCount - dbLoad == 1})");

Pause();

// ── Section 5: TTL expiry ─────────────────────────────────────────────────────

Console.WriteLine("── 5. TTL Expiry — Entry Reloaded After Deadline ──");
Console.WriteLine();

var fakeClock = new FakeClock(DateTimeOffset.UtcNow);
var ttlCache  = new InMemoryCache<string, Book>(fakeClock.Now);
var ttlRepo   = new InMemoryBookRepository();
ttlRepo.Seed([new("bx", "The Stone Angel", "Margaret Laurence", "978-x", 18.99m, "Literary Fiction")]);
var ttlService = new BookCatalogueService(ttlRepo, ttlCache, new InMemoryCache<string, IReadOnlyList<Book>>());

// set book in cache with a 5-minute TTL
ttlRepo.FindById("bx");  // pre-warm repo's LoadCount reference
var dbRef = ttlRepo.LoadCount;
ttlCache.Set("book:bx", new Book("bx", "The Stone Angel", "Margaret Laurence", "978-x", 18.99m, "Literary Fiction"), TimeSpan.FromMinutes(5));

Console.Write("  T+0 min  — cache hit: ");
Console.WriteLine(ttlCache.TryGet("book:bx", out _) ? "YES" : "NO");

fakeClock.Advance(TimeSpan.FromMinutes(4));
Console.Write("  T+4 min  — cache hit: ");
Console.WriteLine(ttlCache.TryGet("book:bx", out _) ? "YES" : "NO");

fakeClock.Advance(TimeSpan.FromMinutes(2));
Console.Write("  T+6 min  — cache hit: ");
Console.WriteLine(ttlCache.TryGet("book:bx", out _) ? "YES (expired)" : "NO (TTL elapsed — entry evicted)");

Pause();

// ── Section 6: Metrics summary ───────────────────────────────────────────────

Console.WriteLine("── 6. Metrics Summary ──");
Console.WriteLine();
Console.WriteLine($"  Total DB queries (main repo) : {repo.LoadCount}");
Console.WriteLine($"  Book cache  — hits: {bookCache.Hits,2}  misses: {bookCache.Misses,2}  live entries: {bookCache.Count}");
Console.WriteLine($"  List cache  — hits: {listCache.Hits,2}  misses: {listCache.Misses,2}  live entries: {listCache.Count}");
Console.WriteLine();
Console.WriteLine("=== End of Demo ===");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}

// ── Helpers ──────────────────────────────────────────────────────────────────

sealed class FakeClock(DateTimeOffset initial)
{
    private DateTimeOffset _current = initial;
    public DateTimeOffset Now() => _current;
    public void Advance(TimeSpan by) => _current = _current.Add(by);
}
