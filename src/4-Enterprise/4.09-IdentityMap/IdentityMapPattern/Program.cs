using Microsoft.Data.Sqlite;
using IdentityMapPattern.Domain;
using IdentityMapPattern.Infrastructure;
using IdentityMapPattern.Mappers;

using var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();
Schema.Create(connection);

var artists  = new ArtistMapper(connection);
var artworks = new ArtworkMapper(connection);

// ── Section 1: Seed the gallery ──────────────────────────────────────────────
Console.WriteLine("=== Vancouver Art Gallery — Identity Map Demo ===\n");
Console.WriteLine("--- Seeding the Gallery ---");

var carr      = artists.Insert(new Artist(0, "Emily Carr",        "Canadian", 1871));
var harris    = artists.Insert(new Artist(0, "Lawren Harris",     "Canadian", 1885));
var colville  = artists.Insert(new Artist(0, "Alex Colville",     "Canadian", 1920));

var bigRaven  = artworks.Insert(new Artwork(0, "Big Raven",              carr.Id,     "Oil on canvas",    1931,   485_000m));
var bcForest  = artworks.Insert(new Artwork(0, "Forest, British Columbia", carr.Id,   "Oil on canvas",    1932,   320_000m));
var lakeSup   = artworks.Insert(new Artwork(0, "Lake Superior",           harris.Id,  "Oil on canvas",    1924, 1_200_000m));
var horseTrain = artworks.Insert(new Artwork(0, "Horse and Train",        colville.Id,"Oil on hardboard", 1954, 2_100_000m));
var churchHill = artworks.Insert(new Artwork(0, "Church and Houses",      colville.Id,"Oil on canvas",    1955,   640_000m));

Console.WriteLine($"Inserted {artists.CacheSize} artists and {artworks.CacheSize} artworks.");
Console.WriteLine($"DB loads so far (artists): {artists.LoadCount}  (artworks): {artworks.LoadCount}");

Pause();

// ── Section 2: The cost without Identity Map ─────────────────────────────────
Console.WriteLine("--- The Cost Without Identity Map ---");
Console.WriteLine("Two ArtworkMapper instances share the same DB but have separate maps:");

var mapperA = new ArtworkMapper(connection);
var mapperB = new ArtworkMapper(connection);
var fromA   = mapperA.FindById(bigRaven.Id)!;
var fromB   = mapperB.FindById(bigRaven.Id)!;

Console.WriteLine($"  mapperA loaded: '{fromA.Title}'");
Console.WriteLine($"  mapperB loaded: '{fromB.Title}'");
Console.WriteLine($"  Same title?  {fromA.Title == fromB.Title}");
Console.WriteLine($"  Same object? {ReferenceEquals(fromA, fromB)}  ← two different instances");

fromA.PutOnDisplay();
Console.WriteLine($"\n  After fromA.PutOnDisplay():");
Console.WriteLine($"    fromA.OnDisplay: {fromA.OnDisplay}");
Console.WriteLine($"    fromB.OnDisplay: {fromB.OnDisplay}  ← stale — fromB doesn't know");

Pause();

// ── Section 3: Identity guarantee ────────────────────────────────────────────
Console.WriteLine("--- Identity Guarantee ---");
Console.WriteLine("Single mapper, two FindById calls for the same Id:");

artworks = new ArtworkMapper(connection);  // fresh mapper, empty cache
var loadCount0 = artworks.LoadCount;
var art1 = artworks.FindById(bigRaven.Id)!;
Console.WriteLine($"  First load  — DB loads: {artworks.LoadCount}  CacheSize: {artworks.CacheSize}");

var art2 = artworks.FindById(bigRaven.Id)!;
Console.WriteLine($"  Second load — DB loads: {artworks.LoadCount}  CacheSize: {artworks.CacheSize}  (no extra DB hit)");

Console.WriteLine($"\n  Same title?  {art1.Title == art2.Title}");
Console.WriteLine($"  ReferenceEquals: {ReferenceEquals(art1, art2)}  ← guaranteed same object");

Pause();

// ── Section 4: In-memory consistency ─────────────────────────────────────────
Console.WriteLine("--- In-Memory Consistency ---");

art1.PutOnDisplay();
Console.WriteLine("Called art1.PutOnDisplay().");
Console.WriteLine($"  art1.OnDisplay: {art1.OnDisplay}");
Console.WriteLine($"  art2.OnDisplay: {art2.OnDisplay}  ← same object, mutation visible everywhere");

var art3 = artworks.FindById(bigRaven.Id)!;
Console.WriteLine($"  art3.OnDisplay: {art3.OnDisplay}  ← third reference, same cached instance");
Console.WriteLine($"  DB loads unchanged: {artworks.LoadCount}  (cache served all three)");

Pause();

// ── Section 5: FindAll populates the map ─────────────────────────────────────
Console.WriteLine("--- FindAll Populates the Map ---");

artworks = new ArtworkMapper(connection);  // fresh mapper
Console.WriteLine($"Before FindAll — CacheSize: {artworks.CacheSize}  DB loads: {artworks.LoadCount}");

var all = artworks.FindAll();
Console.WriteLine($"After FindAll  — CacheSize: {artworks.CacheSize}  DB loads: {artworks.LoadCount}");

var fromCache = artworks.FindById(bigRaven.Id)!;
Console.WriteLine($"FindById after FindAll — DB loads still: {artworks.LoadCount}  (served from map)");
Console.WriteLine($"ReferenceEquals(all[0..4], FindById): {ReferenceEquals(all.First(a => a.Id == bigRaven.Id), fromCache)}");

Pause();

// ── Section 6: Cache eviction on delete ──────────────────────────────────────
Console.WriteLine("--- Cache Eviction on Delete ---");

Console.WriteLine($"CacheSize before delete: {artworks.CacheSize}");
artworks.Delete(bigRaven.Id);
Console.WriteLine($"Deleted '{bigRaven.Title}'.");
Console.WriteLine($"CacheSize after delete:  {artworks.CacheSize}  (evicted from map)");

var afterDelete = artworks.FindById(bigRaven.Id);
Console.WriteLine($"FindById after delete: {(afterDelete is null ? "null — not in DB or map" : afterDelete.Title)}");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
