using CompositePattern;

// ============================================================
// COMPOSITE PATTERN — DEMO
// ============================================================
// Shows how Files (leaves) and Directories (composites) are
// treated uniformly through IFileSystemEntry.
// GetSize(), Display(), and Search() work the same on either.
// ============================================================

using File      = CompositePattern.File;
using Directory = CompositePattern.Directory;

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine("\n");
}

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║           COMPOSITE PATTERN — File System            ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("Files (leaves) and Directories (composites) implement");
Console.WriteLine("the same IFileSystemEntry interface. The client calls");
Console.WriteLine("GetSize() and Display() without knowing which is which.");

Pause();

// ── Build the file system tree ────────────────────────────────

var root = new Directory("project");

var src = new Directory("src");
src.Add(new File("Program.cs",   4_200))
   .Add(new File("Settings.cs",  1_800))
   .Add(new File("Constants.cs",   650));

var controllers = new Directory("Controllers");
controllers.Add(new File("HomeController.cs",   8_100))
           .Add(new File("OrderController.cs", 12_400))
           .Add(new File("UserController.cs",   9_700));

var models = new Directory("Models");
models.Add(new File("Order.cs",   3_200))
      .Add(new File("User.cs",    2_900))
      .Add(new File("Product.cs", 1_750));

src.Add(controllers).Add(models);

var tests = new Directory("tests");
tests.Add(new File("OrderTests.cs",   6_800))
     .Add(new File("UserTests.cs",    5_400))
     .Add(new File("ProductTests.cs", 3_100));

var docs = new Directory("docs");
docs.Add(new File("README.md",      2_048))
    .Add(new File("CHANGELOG.md",   1_024))
    .Add(new File("API.md",         8_192));

root.Add(src).Add(tests).Add(docs);
root.Add(new File(".gitignore",  512))
    .Add(new File("LICENSE",     1_024));

// ── DEMO 1: Display the full tree ────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 1 — Full directory tree with sizes");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
root.Display();

Pause();

// ── DEMO 2: Uniform GetSize() ─────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 2 — GetSize() works uniformly on files and directories");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("The client uses IFileSystemEntry — it doesn't know the type underneath.");
Console.WriteLine();

IFileSystemEntry[] entries =
[
    new File("standalone.log", 52_428_800),  // a single file
    src,                                      // an entire directory tree
    root                                      // the whole project
];

foreach (var entry in entries)
{
    // Same call — IFileSystemEntry.GetSize() — regardless of type
    Console.WriteLine($"  {entry.Name,-20}  {entry.GetSize(),12:N0} bytes");
}

Pause();

// ── DEMO 3: Search ────────────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 3 — Search traverses the entire tree uniformly");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

// Find all .cs files
Console.WriteLine("All .cs files in the project:");
var csFiles = root.Search(e => e is File f && f.Name.EndsWith(".cs"));
foreach (var entry in csFiles)
    Console.WriteLine($"  {entry.Name} ({entry.GetSize():N0} bytes)");

Console.WriteLine();

// Find all entries larger than 8 KB
Console.WriteLine("Everything larger than 8 KB:");
var largeEntries = root.Search(e => e.GetSize() > 8_192);
foreach (var entry in largeEntries)
    Console.WriteLine($"  {entry.Name} ({entry.GetSize():N0} bytes)");

Pause();

// ── DEMO 4: Treating a subtree as the root ────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 4 — Any subtree can be used as the root");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("Pass a subdirectory anywhere IFileSystemEntry is expected.");
Console.WriteLine("The caller doesn't know it's not the root.");
Console.WriteLine();

// A method that just takes IFileSystemEntry — leaf or composite
static void PrintSummary(IFileSystemEntry entry)
{
    Console.WriteLine($"  Entry  : {entry.Name}");
    Console.WriteLine($"  Size   : {entry.GetSize():N0} bytes");
    Console.WriteLine($"  Matches: {entry.Search(e => e is File).Count()} files total");
}

Console.WriteLine("Passing the 'src/Controllers' subdirectory:");
PrintSummary(controllers);
Console.WriteLine();
Console.WriteLine("Passing a single file:");
PrintSummary(new File("Report.pdf", 2_097_152));

Pause();

// ── DEMO 5: The problem without Composite ─────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 5 — Why Composite? Avoiding type-checking.");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("WITHOUT Composite, every caller would need to do this:");
Console.WriteLine();
Console.WriteLine("  if (entry is File f)");
Console.WriteLine("      total += f.Size;");
Console.WriteLine("  else if (entry is Directory d)");
Console.WriteLine("      total += d.RecursiveSum();   // different method!");
Console.WriteLine();
Console.WriteLine("WITH Composite, the recursive logic is encapsulated once:");
Console.WriteLine();
Console.WriteLine("  total += entry.GetSize();        // works for both, always");
Console.WriteLine();
Console.WriteLine("This matters most when building tools that traverse trees:");
Console.WriteLine("  disk analysers, UI component renderers, org chart walkers,");
Console.WriteLine("  expression evaluators, scene graph engines.");
Console.WriteLine();
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("Summary:");
Console.WriteLine("  • File (leaf): answers only for itself");
Console.WriteLine("  • Directory (composite): delegates to children, aggregates");
Console.WriteLine("  • Client uses IFileSystemEntry — never checks the type");
Console.WriteLine("  • Any node can be treated as the root of its own subtree");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
