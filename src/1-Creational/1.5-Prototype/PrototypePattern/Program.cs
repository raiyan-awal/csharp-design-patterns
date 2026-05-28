namespace PrototypePattern;

/// <summary>
/// PROTOTYPE PATTERN DEMONSTRATION
///
/// This program demonstrates the Prototype pattern using a game enemy spawner.
/// It shows:
/// 1. Basic cloning — clone a template and modify the copy independently
/// 2. The shallow copy trap — shared references cause unexpected mutations
/// 3. Deep clone — fully independent copies, no shared state
/// 4. Prototype Registry — named templates, spawning at runtime
/// 5. Prototype vs new — when cloning beats constructing from scratch
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== PROTOTYPE PATTERN DEMO ===");
        Console.WriteLine("Press any key after each section to continue.\n");

        // ── DEMONSTRATION 1: Basic cloning ───────────────────────────────────
        Console.WriteLine("--- Demonstration 1: Basic Cloning ---");
        Console.WriteLine("Define a Goblin template once, then spawn two independent copies.\n");

        var goblinTemplate = new Enemy
        {
            Name      = "Goblin",
            Faction   = "Dark Horde",
            Health    = 80,
            Damage    = 12,
            Equipment = ["Rusty Dagger", "Leather Vest"],
            Stats     = new CombatStats { Armor = 5, Speed = 8, XpReward = 25 }
        };

        var goblin1 = goblinTemplate.DeepClone();
        var goblin2 = goblinTemplate.DeepClone();

        // Customise each spawn independently
        goblin1.Name   = "Goblin Scout";
        goblin1.Health = 60;

        goblin2.Name   = "Goblin Brute";
        goblin2.Damage = 20;
        goblin2.Equipment.Add("Spiked Club");

        Console.WriteLine($"Template : {goblinTemplate}");
        Console.WriteLine($"Spawn 1  : {goblin1}");
        Console.WriteLine($"Spawn 2  : {goblin2}");

        Console.WriteLine("\n→ Template is unchanged despite modifications to both spawns.");
        Console.WriteLine("→ Each spawn started from the same base but diverged independently.");

        Pause();

        // ── DEMONSTRATION 2: The shallow copy trap ───────────────────────────
        Console.WriteLine("--- Demonstration 2: The Shallow Copy Trap ---");
        Console.WriteLine("ShallowClone() copies references, not the objects behind them.\n");

        var trollTemplate = new Enemy
        {
            Name      = "Troll",
            Faction   = "Cave Clan",
            Health    = 300,
            Damage    = 35,
            Equipment = ["Stone Axe", "Bone Shield"],
            Stats     = new CombatStats { Armor = 20, Speed = 3, XpReward = 120 }
        };

        var shallowTroll = trollTemplate.ShallowClone();

        Console.WriteLine("Before modification:");
        Console.WriteLine($"  Template equipment : [{string.Join(", ", trollTemplate.Equipment)}]");
        Console.WriteLine($"  Shallow clone equip: [{string.Join(", ", shallowTroll.Equipment)}]");

        // Add an item to the clone's equipment
        shallowTroll.Equipment.Add("Stolen Helmet");
        shallowTroll.Stats.Armor = 99; // Also mutate the nested Stats object

        Console.WriteLine("\nAfter adding 'Stolen Helmet' to the shallow clone:");
        Console.WriteLine($"  Template equipment : [{string.Join(", ", trollTemplate.Equipment)}]");
        Console.WriteLine($"  Shallow clone equip: [{string.Join(", ", shallowTroll.Equipment)}]");
        Console.WriteLine($"\n  Template armor     : {trollTemplate.Stats.Armor}");
        Console.WriteLine($"  Shallow clone armor: {shallowTroll.Stats.Armor}");

        Console.WriteLine("\n→ PROBLEM: The template was mutated through the clone!");
        Console.WriteLine("→ Both share the SAME List<string> and CombatStats objects in memory.");
        Console.WriteLine("→ Value-type fields (Health, Damage) are independent — only references are shared.");

        Pause();

        // ── DEMONSTRATION 3: Deep clone ──────────────────────────────────────
        Console.WriteLine("--- Demonstration 3: Deep Clone — Fully Independent Copies ---");
        Console.WriteLine("DeepClone() allocates new objects for every reference-type field.\n");

        var dragonTemplate = new Enemy
        {
            Name      = "Dragon",
            Faction   = "Ancient",
            Health    = 2000,
            Damage    = 150,
            Equipment = ["Dragon Scales", "Fire Breath"],
            Stats     = new CombatStats { Armor = 80, Speed = 5, XpReward = 1000 }
        };

        var deepDragon = dragonTemplate.DeepClone();

        Console.WriteLine("Before modification:");
        Console.WriteLine($"  Template equipment : [{string.Join(", ", dragonTemplate.Equipment)}]");
        Console.WriteLine($"  Deep clone equip   : [{string.Join(", ", deepDragon.Equipment)}]");
        Console.WriteLine($"  Same list object?  : {ReferenceEquals(dragonTemplate.Equipment, deepDragon.Equipment)}");
        Console.WriteLine($"  Same Stats object? : {ReferenceEquals(dragonTemplate.Stats, deepDragon.Stats)}");

        deepDragon.Equipment.Add("Enchanted Collar");
        deepDragon.Stats.Armor = 50;

        Console.WriteLine("\nAfter modifying the deep clone:");
        Console.WriteLine($"  Template equipment : [{string.Join(", ", dragonTemplate.Equipment)}]");
        Console.WriteLine($"  Deep clone equip   : [{string.Join(", ", deepDragon.Equipment)}]");
        Console.WriteLine($"  Template armor     : {dragonTemplate.Stats.Armor}");
        Console.WriteLine($"  Deep clone armor   : {deepDragon.Stats.Armor}");

        Console.WriteLine("\n→ Template is completely unaffected — true independence.");
        Console.WriteLine("→ Equipment and Stats are separate objects in memory.");

        Pause();

        // ── DEMONSTRATION 4: Prototype Registry ──────────────────────────────
        Console.WriteLine("--- Demonstration 4: Prototype Registry ---");
        Console.WriteLine("A catalogue of named templates — spawn by name, get an independent copy.\n");

        var registry = new EnemyRegistry();

        registry.Register("goblin", new Enemy
        {
            Name      = "Goblin",
            Faction   = "Dark Horde",
            Health    = 80,
            Damage    = 12,
            Equipment = ["Rusty Dagger"],
            Stats     = new CombatStats { Armor = 5, Speed = 8, XpReward = 25 }
        });

        registry.Register("troll", new Enemy
        {
            Name      = "Troll",
            Faction   = "Cave Clan",
            Health    = 300,
            Damage    = 35,
            Equipment = ["Stone Axe", "Bone Shield"],
            Stats     = new CombatStats { Armor = 20, Speed = 3, XpReward = 120 }
        });

        registry.Register("dragon", new Enemy
        {
            Name      = "Dragon",
            Faction   = "Ancient",
            Health    = 2000,
            Damage    = 150,
            Equipment = ["Dragon Scales", "Fire Breath"],
            Stats     = new CombatStats { Armor = 80, Speed = 5, XpReward = 1000 }
        });

        Console.WriteLine($"Registered {registry.Count} templates: [{string.Join(", ", registry.Keys)}]\n");

        Console.WriteLine("Spawning a wave of enemies for Zone 1:");
        var wave = new[]
        {
            registry.Spawn("goblin"),
            registry.Spawn("goblin"),
            registry.Spawn("troll"),
            registry.Spawn("goblin"),
            registry.Spawn("dragon"),
        };

        foreach (var enemy in wave)
            Console.WriteLine($"  + Spawned: {enemy.Name} (HP:{enemy.Health})");

        Console.WriteLine("\nCustomising the spawned dragon (giving it a title):");
        wave[4].Name = "Ancient Wyrm";
        wave[4].Health = 3000;
        Console.WriteLine($"  Spawn: {wave[4].Name} HP:{wave[4].Health}");

        var freshDragon = registry.Spawn("dragon");
        Console.WriteLine($"  Next dragon spawn from registry: {freshDragon.Name} HP:{freshDragon.Health}");
        Console.WriteLine("\n→ Registry template is untouched — the customised spawn is its own object.");

        Console.WriteLine("\nAttempting to spawn an unregistered enemy:");
        try
        {
            registry.Spawn("lich");
        }
        catch (KeyNotFoundException ex)
        {
            Console.WriteLine($"  ✓ KeyNotFoundException: {ex.Message}");
        }

        Pause();

        // ── DEMONSTRATION 5: Prototype vs new ────────────────────────────────
        Console.WriteLine("--- Demonstration 5: Prototype vs Constructing with 'new' ---");
        Console.WriteLine("When does cloning beat constructing from scratch?\n");

        Console.WriteLine("SCENARIO: Spawning 1,000 goblins for a dungeon level.");

        Console.WriteLine("""

  With 'new' (no prototype):
    for (int i = 0; i < 1000; i++)
    {
        var g = new Enemy
        {
            Name      = "Goblin",
            Faction   = "Dark Horde",
            Health    = 80,
            Damage    = 12,
            Equipment = new List<string> { "Rusty Dagger", "Leather Vest" },
            Stats     = new CombatStats { Armor = 5, Speed = 8, XpReward = 25 }
        };
        // Any change to the base stats needs to be found and edited in 1000 places
    }
""");

        Console.WriteLine("""
  With Prototype + Registry:
    registry.Register("goblin", goblinTemplate);  // configure once

    for (int i = 0; i < 1000; i++)
    {
        var g = registry.Spawn("goblin");  // clone from the one source of truth
        // Change stats on the template → all future spawns get the update
    }
""");

        Console.WriteLine("USE PROTOTYPE WHEN:");
        Console.WriteLine("  ✓ Constructing an object is expensive (deep object graph, DB lookup, calculation)");
        Console.WriteLine("  ✓ You need many similar objects that differ only in a few fields");
        Console.WriteLine("  ✓ The class hierarchy is complex and you want to copy without knowing the concrete type");
        Console.WriteLine("  ✓ You want a single source of truth for default values (the template)");

        Console.WriteLine("\nUSE 'new' WHEN:");
        Console.WriteLine("  ✓ Construction is cheap and objects are small");
        Console.WriteLine("  ✓ Every instance is fundamentally different (no shared starting state)");
        Console.WriteLine("  ✓ The type is known at compile time and DI can wire it up");

        Pause();

        // ── SUMMARY ──────────────────────────────────────────────────────────
        Console.WriteLine("=== KEY TAKEAWAYS ===");
        Console.WriteLine("✓ Prototype clones an existing object instead of constructing from scratch");
        Console.WriteLine("✓ ShallowClone (MemberwiseClone) copies value types but SHARES reference types");
        Console.WriteLine("✓ DeepClone replaces each reference-type field with a new independent copy");
        Console.WriteLine("✓ A Registry stores named templates and hands out deep clones on Spawn()");
        Console.WriteLine("✓ The template is always protected — callers never get a direct reference to it");

        Console.WriteLine("\n=== REAL-WORLD USAGE IN .NET ===");
        Console.WriteLine("• Object.MemberwiseClone() — built-in shallow clone (protected method)");
        Console.WriteLine("• ICloneable — .NET's built-in interface (ambiguous shallow/deep, avoid in new code)");
        Console.WriteLine("• JSON round-trip cloning: JsonSerializer.Deserialize(JsonSerializer.Serialize(obj))");
        Console.WriteLine("• AutoMapper — cloning / projecting between object shapes");
        Console.WriteLine("• EF Core: AsNoTracking() returns detached copies of tracked entities");
        Console.WriteLine("• ASP.NET Core: IOptions<T> snapshot — a cloned copy of config at request time");

        Console.WriteLine("\nDemo complete.");
    }

    static void Pause()
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey(intercept: true);
        Console.WriteLine();
    }
}
