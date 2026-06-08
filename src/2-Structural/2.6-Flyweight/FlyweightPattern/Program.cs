using FlyweightPattern;

// ============================================================
// FLYWEIGHT PATTERN — DEMO
// ============================================================
// Shows how a forest of many trees shares TreeType flyweights.
// Intrinsic state (species, colour, texture) is stored once
// per unique type. Extrinsic state (position, height, age) is
// stored per tree and passed to the flyweight at render time.
// ============================================================

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine("\n");
}

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║       FLYWEIGHT PATTERN — Forest Simulation          ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("Thousands of trees share TreeType objects (species,");
Console.WriteLine("colour, texture). Only position, height, and age are");
Console.WriteLine("stored per tree. The factory ensures one TreeType per");
Console.WriteLine("unique species — regardless of how many trees use it.");

Pause();

// ── DEMO 1: Small forest — watch the factory at work ─────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 1 — Plant 9 trees of 3 species");
Console.WriteLine("         (factory log shows when a new type is created)");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var forest = new Forest();

// Each call to PlantTree with a new species triggers one factory allocation.
// Same species → same cached instance.
forest.PlantTree( 10,  20,  500, 30, "Oak",   "dark green",   "oak_bark.png");
forest.PlantTree( 15,  45,  750, 50, "Oak",   "dark green",   "oak_bark.png");  // cached
forest.PlantTree( 80,  10,  620, 40, "Oak",   "dark green",   "oak_bark.png");  // cached

forest.PlantTree( 30,  60,  300, 15, "Pine",  "bright green", "pine_bark.png");
forest.PlantTree( 55,  75,  420, 22, "Pine",  "bright green", "pine_bark.png");  // cached
forest.PlantTree( 90,  35,  350, 18, "Pine",  "bright green", "pine_bark.png");  // cached

forest.PlantTree( 20,  80, 1200, 80, "Birch", "pale yellow",  "birch_bark.png");
forest.PlantTree( 65,  55,  900, 60, "Birch", "pale yellow",  "birch_bark.png");  // cached
forest.PlantTree( 40,  25,  800, 55, "Birch", "pale yellow",  "birch_bark.png");  // cached

Console.WriteLine();
Console.WriteLine($"Trees planted : {forest.TreeCount}");
Console.WriteLine($"TreeType objects in memory: {forest.UniqueTypeCount}");
Console.WriteLine();
Console.WriteLine("Rendering all trees:");
forest.Draw();

Pause();

// ── DEMO 2: Prove sharing with ReferenceEquals ───────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 2 — Prove sharing: all Oak trees hold the SAME object");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var oaks = forest.Trees.Where(t => t.Species == "Oak").ToList();

Console.WriteLine($"Oak trees: {oaks.Count}");
Console.WriteLine();

for (int i = 1; i < oaks.Count; i++)
{
    bool same = ReferenceEquals(oaks[0].Type, oaks[i].Type);
    Console.WriteLine($"  Oak[0].Type == Oak[{i}].Type  →  {same}");
}

Console.WriteLine();
Console.WriteLine("Each Oak has different (X, Y, HeightCm, AgeYears) but identical Type:");
Console.WriteLine($"  TreeType hash: {oaks[0].Type.GetHashCode()}  (same for all Oaks)");
Console.WriteLine($"  Oak[0] at ({oaks[0].X},{oaks[0].Y})  h={oaks[0].HeightCm}cm");
Console.WriteLine($"  Oak[1] at ({oaks[1].X},{oaks[1].Y})  h={oaks[1].HeightCm}cm");
Console.WriteLine($"  Oak[2] at ({oaks[2].X},{oaks[2].Y})  h={oaks[2].HeightCm}cm");

Pause();

// ── DEMO 3: Large-scale — memory savings ─────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 3 — Large-scale forest: 100,000 trees, 5 species");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var bigForest = new Forest();
var rng       = new Random(42);

string[][] species =
[
    ["Oak",    "dark green",    "oak_bark.png"],
    ["Pine",   "bright green",  "pine_bark.png"],
    ["Birch",  "pale yellow",   "birch_bark.png"],
    ["Maple",  "orange-red",    "maple_bark.png"],
    ["Spruce", "blue-green",    "spruce_bark.png"],
];

Console.Write("Planting 100,000 trees");
for (int i = 0; i < 100_000; i++)
{
    var s = species[rng.Next(species.Length)];
    bigForest.PlantTree(
        rng.Next(10_000), rng.Next(10_000),
        rng.Next(200, 2000), rng.Next(1, 100),
        s[0], s[1], s[2]);

    if (i % 10_000 == 9_999) Console.Write(".");
}

Console.WriteLine(" done.");
Console.WriteLine();
Console.WriteLine($"Trees planted      : {bigForest.TreeCount:N0}");
Console.WriteLine($"TreeType objects   : {bigForest.UniqueTypeCount}");
Console.WriteLine();

// Approximate memory calculation
const int treeContextBytes   = 4 * 4 + 8;          // 4 ints + 1 reference = 24 bytes
const int intrinsicBytesEach = 3 * (50 + 24);       // ~3 strings × (avg chars + object header)

long withFlyweight    = (long)bigForest.TreeCount * treeContextBytes
                        + bigForest.UniqueTypeCount * intrinsicBytesEach;
long withoutFlyweight = (long)bigForest.TreeCount * (treeContextBytes + intrinsicBytesEach);
long saved            = withoutFlyweight - withFlyweight;

Console.WriteLine("Approximate memory for tree data:");
Console.WriteLine($"  Without Flyweight : {withoutFlyweight / 1024.0:F1} KB");
Console.WriteLine($"  With    Flyweight : {withFlyweight    / 1024.0:F1} KB");
Console.WriteLine($"  Saved             : {saved            / 1024.0:F1} KB  ({100.0 * saved / withoutFlyweight:F1}% reduction)");

Pause();

// ── DEMO 4: The problem without Flyweight ────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 4 — Without Flyweight: every tree owns its own copy");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("Without Flyweight, each tree object would look like:");
Console.WriteLine();
Console.WriteLine("  class Tree {");
Console.WriteLine("      int    X, Y, HeightCm, AgeYears;   ← unique (24 bytes)");
Console.WriteLine("      string Species  = \"Oak\";            ← duplicated in every Oak");
Console.WriteLine("      string Colour   = \"dark green\";     ← duplicated in every Oak");
Console.WriteLine("      string Texture  = \"oak_bark.png\";   ← duplicated in every Oak");
Console.WriteLine("  }");
Console.WriteLine();
Console.WriteLine("With Flyweight:");
Console.WriteLine();
Console.WriteLine("  class Tree {");
Console.WriteLine("      int      X, Y, HeightCm, AgeYears;  ← unique  (24 bytes)");
Console.WriteLine("      TreeType _type;                      ← shared   (8-byte reference)");
Console.WriteLine("  }");
Console.WriteLine("  // 1 TreeType object shared by all 20,000 Oaks");

Console.WriteLine();
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("Summary:");
Console.WriteLine("  • TreeType (flyweight)  — intrinsic state, shared, immutable");
Console.WriteLine("  • Tree (context)        — extrinsic state unique per instance");
Console.WriteLine("  • TreeTypeFactory       — cache; one instance per unique key");
Console.WriteLine("  • Forest               — container; uses factory transparently");
Console.WriteLine("  • 100,000 trees → only 5 TreeType objects in memory");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
