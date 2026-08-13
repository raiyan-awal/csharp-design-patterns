using Microsoft.Data.Sqlite;
using DataMapperPattern.Domain;
using DataMapperPattern.Mappers;

Console.WriteLine("=== 4.07 Data Mapper Pattern — Canadian Film Registry ===");
Console.WriteLine();

// ── The Problem Without Data Mapper ──────────────────────────────────────────
Console.WriteLine("── The Problem Without Data Mapper ──────────────────────────────────────────");
Console.WriteLine();
Console.WriteLine("  Without Data Mapper, domain objects know about the database — mixing");
Console.WriteLine("  two very different concerns in one class:");
Console.WriteLine();
Console.WriteLine("  // Active Record style — the object saves itself");
Console.WriteLine("  public class Film");
Console.WriteLine("  {");
Console.WriteLine("      private static SqliteConnection _db = new(connectionString);");
Console.WriteLine();
Console.WriteLine("      public string Title { get; set; }");
Console.WriteLine("      public string Director { get; set; }");
Console.WriteLine();
Console.WriteLine("      public static Film? Find(int id) =>   // DB knowledge in domain");
Console.WriteLine("          _db.QuerySingle<Film>(\"SELECT * FROM Films WHERE Id = @id\", ...)");
Console.WriteLine();
Console.WriteLine("      public void Save() =>                  // DB knowledge in domain");
Console.WriteLine("          _db.Execute(\"INSERT INTO Films ...\", this);");
Console.WriteLine("  }");
Console.WriteLine();
Console.WriteLine("  Change the table name or column layout and you edit Film.");
Console.WriteLine("  Change a business rule and you risk breaking the SQL.");
Console.WriteLine("  The two concerns cannot evolve independently.");

Pause();

// ── Setup ─────────────────────────────────────────────────────────────────────
using var db = new SqliteConnection("Data Source=:memory:");
db.Open();
Schema.Create(db);

var films   = new FilmMapper(db);
var reviews = new ReviewMapper(db);

// ── Demo 1: Pure Domain Objects ───────────────────────────────────────────────
Console.WriteLine("── Demo 1: Pure Domain Objects ──────────────────────────────────────────────");
Console.WriteLine();
Console.WriteLine("  Film is a plain C# class — no SQL, no Dapper attributes, no base class.");
Console.WriteLine("  It owns only domain logic: Certify() / Decertify().");
Console.WriteLine();

// Domain objects created in memory — no DB involved yet
var atanarjuat = new Film(0, "Atanarjuat: The Fast Runner", "Zacharias Kunuk",
                           "Drama", 2001, 172);
var incendies  = new Film(0, "Incendies",                   "Denis Villeneuve",
                           "Thriller", 2010, 130);
var sweetHere  = new Film(0, "The Sweet Hereafter",         "Atom Egoyan",
                           "Drama", 1997, 112);
var monsieur   = new Film(0, "Monsieur Lazhar",             "Philippe Falardeau",
                           "Drama", 2011, 94);
var bonCop     = new Film(0, "Bon Cop, Bad Cop",            "Érik Canuel",
                           "Comedy", 2006, 116);

Console.WriteLine($"  Domain object created in memory: '{atanarjuat.Title}'");
Console.WriteLine($"    CertifiedFresh = {atanarjuat.CertifiedFresh}");
atanarjuat.Certify();
Console.WriteLine($"    After Certify() = {atanarjuat.CertifiedFresh}");
Console.WriteLine($"  No database touched — Film knows nothing about storage.");

Pause();

// ── Demo 2: Inserting via the Mapper ─────────────────────────────────────────
Console.WriteLine("── Demo 2: Inserting via the Mapper ─────────────────────────────────────────");
Console.WriteLine();

atanarjuat = films.Insert(atanarjuat);
incendies  = films.Insert(incendies);
sweetHere  = films.Insert(sweetHere);
monsieur   = films.Insert(monsieur);
bonCop     = films.Insert(bonCop);

Console.WriteLine("  Mapper translates Film → SQL INSERT, returns Film with assigned Id:");
foreach (var f in films.FindAll())
    Console.WriteLine($"    [{f.Id}] {f.ReleaseYear}  {f.Title,-38} dir. {f.Director}");

Pause();

// ── Demo 3: Querying ──────────────────────────────────────────────────────────
Console.WriteLine("── Demo 3: Querying ─────────────────────────────────────────────────────────");
Console.WriteLine();

var found = films.FindById(incendies.Id);
Console.WriteLine($"  FindById({incendies.Id}): '{found?.Title}' ({found?.ReleaseYear})");

var notFound = films.FindById(999);
Console.WriteLine($"  FindById(999): {notFound?.Title ?? "null — not found"}");

Console.WriteLine();
Console.WriteLine("  Drama films:");
foreach (var f in films.FindByGenre("Drama"))
    Console.WriteLine($"    {f.Title} ({f.ReleaseYear})");

Console.WriteLine();
Console.WriteLine("  Denis Villeneuve films:");
foreach (var f in films.FindByDirector("Denis Villeneuve"))
    Console.WriteLine($"    {f.Title} ({f.ReleaseYear})");

Pause();

// ── Demo 4: Updating ──────────────────────────────────────────────────────────
Console.WriteLine("── Demo 4: Updating ─────────────────────────────────────────────────────────");
Console.WriteLine();

Console.WriteLine($"  Before: '{incendies.Title}' CertifiedFresh = {incendies.CertifiedFresh}");
incendies.Certify();
films.Update(incendies);
var reloaded = films.FindById(incendies.Id)!;
Console.WriteLine($"  After update + reload: CertifiedFresh = {reloaded.CertifiedFresh}");
Console.WriteLine();

Console.WriteLine($"  Deleting '{bonCop.Title}'...");
films.Delete(bonCop.Id);
Console.WriteLine($"  Films remaining: {films.FindAll().Count}");

Pause();

// ── Demo 5: Reviews ───────────────────────────────────────────────────────────
Console.WriteLine("── Demo 5: Reviews ──────────────────────────────────────────────────────────");
Console.WriteLine();

var r1 = reviews.Insert(new Review(0, atanarjuat.Id, "Marie-Claire Ouellet",  9, "A landmark of world cinema.", DateTime.UtcNow.AddDays(-10)));
var r2 = reviews.Insert(new Review(0, atanarjuat.Id, "James Whitfield",       8, "Breathtaking and haunting.",  DateTime.UtcNow.AddDays(-5)));
var r3 = reviews.Insert(new Review(0, incendies.Id,  "Sophie Tremblay",      10, "Villeneuve at his finest.",   DateTime.UtcNow.AddDays(-3)));
var r4 = reviews.Insert(new Review(0, incendies.Id,  "David Chen",            9, "Devastating and beautiful.",  DateTime.UtcNow.AddDays(-1)));

Console.WriteLine($"  Reviews for '{atanarjuat.Title}':");
foreach (var r in reviews.FindByFilmId(atanarjuat.Id))
    Console.WriteLine($"    [{r.Score}/10] {r.ReviewerName}: \"{r.Comment}\"");
Console.WriteLine($"    Average: {reviews.AverageScore(atanarjuat.Id):F1}/10");

Console.WriteLine();
Console.WriteLine($"  Reviews for '{incendies.Title}':");
foreach (var r in reviews.FindByFilmId(incendies.Id))
    Console.WriteLine($"    [{r.Score}/10] {r.ReviewerName}: \"{r.Comment}\"");
Console.WriteLine($"    Average: {reviews.AverageScore(incendies.Id):F1}/10");

Pause();

// ── Demo 6: Schema Independence ───────────────────────────────────────────────
Console.WriteLine("── Demo 6: Schema Independence ──────────────────────────────────────────────");
Console.WriteLine();
Console.WriteLine("  The DB schema and the domain object are independent.");
Console.WriteLine("  Here we add an 'AddedOn' audit column to the Films table.");
Console.WriteLine("  The Film class does not change — only the mapper SQL changes.");
Console.WriteLine();

using var db2 = new SqliteConnection("Data Source=:memory:");
db2.Open();
Schema.Create(db2);

// Simulate a schema migration: add an audit column
using (var cmd = db2.CreateCommand())
{
    cmd.CommandText = "ALTER TABLE Films ADD COLUMN AddedOn TEXT NOT NULL DEFAULT '2024-01-01'";
    cmd.ExecuteNonQuery();
}

// The FilmMapper still works unchanged — it selects only the columns it knows about
var films2 = new FilmMapper(db2);
var testFilm = films2.Insert(new Film(0, "Atanarjuat: The Fast Runner", "Zacharias Kunuk",
                                       "Drama", 2001, 172, true));
var retrieved = films2.FindById(testFilm.Id);
Console.WriteLine($"  Schema has extra 'AddedOn' column. FilmMapper still loads Film correctly.");
Console.WriteLine($"  Retrieved: [{retrieved?.Id}] '{retrieved?.Title}'  CertifiedFresh={retrieved?.CertifiedFresh}");
Console.WriteLine();
Console.WriteLine("  This is the core benefit: domain and schema can evolve at different rates.");

Console.WriteLine();
Console.WriteLine("=== End of demo ===");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
