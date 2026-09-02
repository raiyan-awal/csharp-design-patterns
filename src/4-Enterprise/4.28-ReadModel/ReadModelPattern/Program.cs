using ReadModelPattern.Engine;
using ReadModelPattern.Infrastructure;
using ReadModelPattern.Projections;
using ReadModelPattern.ReadModels;
using ReadModelPattern.Services;

// Composition root
var eventStore     = new InMemoryEventStore();
var catalogueStore = new InMemoryReadModelStore<string, ProductCatalogueView>();
var sellerStore    = new InMemoryReadModelStore<string, SellerSummaryView>();

var engine = new ProjectionEngine(eventStore);
engine.Register(new ProductCatalogueProjection(catalogueStore));
engine.Register(new SellerSummaryProjection(sellerStore));

var market = new MarketplaceService(engine, catalogueStore, sellerStore);

Console.WriteLine("=== 4.28 Read Model / Projection — Maple Market ===\n");

// ─── 1. List products ─────────────────────────────────────────────────────────
Console.WriteLine("─── 1. Listing products ───");
market.ListProduct("prod-roots-hoodie",  "seller-roots",    "Roots Canada Hoodie",   89.99m,  50);
market.ListProduct("prod-mec-boots",     "seller-mec",      "MEC Hiking Boots",     149.95m,  30);
market.ListProduct("prod-tims-card",     "seller-roots",    "Tim Hortons Gift Card",  25.00m, 200);
market.ListProduct("prod-cg-toque",      "seller-mec",      "Canada Goose Toque",   119.00m,  20);
Console.WriteLine($"  {catalogueStore.Count} products listed. Event store: {eventStore.Count} events.");

Pause();

// ─── 2. Record sales ─────────────────────────────────────────────────────────
Console.WriteLine("─── 2. Recording sales ───");
market.RecordSale("prod-roots-hoodie", "seller-roots", 12, 89.99m);
market.RecordSale("prod-mec-boots",    "seller-mec",    5, 149.95m);
market.RecordSale("prod-tims-card",    "seller-roots",  40, 25.00m);
market.RecordSale("prod-roots-hoodie", "seller-roots",   8, 89.99m);
market.RecordSale("prod-cg-toque",     "seller-mec",     7, 119.00m);
Console.WriteLine($"  {eventStore.Count} total events in store.");

Pause();

// ─── 3. Price update ─────────────────────────────────────────────────────────
Console.WriteLine("─── 3. Updating a price ───");
market.UpdatePrice("prod-mec-boots", 134.95m);
var boots = market.GetProduct("prod-mec-boots")!;
Console.WriteLine($"  MEC Hiking Boots new price: ${boots.PriceCAD}");

Pause();

// ─── 4. Post reviews ─────────────────────────────────────────────────────────
Console.WriteLine("─── 4. Posting reviews ───");
market.PostReview("prod-roots-hoodie", 5);
market.PostReview("prod-roots-hoodie", 4);
market.PostReview("prod-roots-hoodie", 5);
market.PostReview("prod-mec-boots",    3);
market.PostReview("prod-cg-toque",     5);
market.PostReview("prod-cg-toque",     4);

Pause();

// ─── 5. Product catalogue read model ─────────────────────────────────────────
Console.WriteLine("─── 5. Product catalogue read model ───");
Console.WriteLine($"  {"Product",-28} {"Price":>8}  {"Stock":>5}  {"Sold":>5}  {"Rating":>7}");
Console.WriteLine($"  {new string('-', 60)}");
foreach (var p in market.GetAllProducts().OrderByDescending(p => p.TotalSold))
    Console.WriteLine($"  {p.Title,-28} {p.PriceCAD,8:C}  {p.StockRemaining,5}  {p.TotalSold,5}  {p.AverageRating,7:F1}");

Pause();

// ─── 6. Seller summaries ─────────────────────────────────────────────────────
Console.WriteLine("─── 6. Seller dashboard (aggregated by seller) ───");
foreach (var s in market.GetAllSellerSummaries().OrderByDescending(s => s.TotalRevenueCAD))
    Console.WriteLine($"  {s.SellerId,-18}  Listings: {s.ActiveListings}  Units: {s.TotalUnitsSold}  Revenue: {s.TotalRevenueCAD:C}");

Pause();

// ─── 7. Top selling ──────────────────────────────────────────────────────────
Console.WriteLine("─── 7. Top 2 selling products ───");
var top = market.GetTopSelling(2);
for (var i = 0; i < top.Count; i++)
    Console.WriteLine($"  #{i + 1}  {top[i].Title}  ({top[i].TotalSold} sold)");

Pause();

// ─── 8. Rebuild — add a new projection mid-flight ────────────────────────────
Console.WriteLine("─── 8. Adding a new projection and rebuilding ───");
var newSellerStore = new InMemoryReadModelStore<string, SellerSummaryView>();
engine.Register(new SellerSummaryProjection(newSellerStore));
Console.WriteLine($"  New seller store before rebuild: {newSellerStore.Count} entries");
engine.Rebuild();
Console.WriteLine($"  New seller store after rebuild:  {newSellerStore.Count} entries");
var rootsNew = newSellerStore.Get("seller-roots");
Console.WriteLine($"  seller-roots revenue (rebuilt): {rootsNew?.TotalRevenueCAD:C}");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
