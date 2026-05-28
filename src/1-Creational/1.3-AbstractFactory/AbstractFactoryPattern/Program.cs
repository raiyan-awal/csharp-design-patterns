namespace AbstractFactoryPattern;

/// <summary>
/// ABSTRACT FACTORY PATTERN DEMONSTRATION
///
/// This program demonstrates the Abstract Factory pattern by showing:
/// 1. How factories produce families of related objects
/// 2. How the client is completely decoupled from concrete types
/// 3. How switching factories re-themes the entire UI
/// 4. Why mixing products from different families breaks consistency
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== ABSTRACT FACTORY PATTERN DEMO ===");
        Console.WriteLine("Press any key after each section to continue.\n");

        // DEMONSTRATION 1: Light Theme
        Console.WriteLine("--- Demonstration 1: Light Theme Factory ---");
        Console.WriteLine("Creating a UIRenderer with the Light theme factory.\n");

        IUIFactory lightFactory = new LightThemeFactory();
        var lightRenderer = new UIRenderer(lightFactory);

        Console.WriteLine("Rendering Login Form (Light Theme):");
        lightRenderer.RenderLoginForm();

        Console.WriteLine("\nRendering Settings Panel (Light Theme):");
        lightRenderer.RenderSettingsPanel();

        Console.WriteLine("\n→ Every component came from LightThemeFactory — all visually consistent.");

        Pause();

        // DEMONSTRATION 2: Dark Theme — same client code, different factory
        Console.WriteLine("--- Demonstration 2: Dark Theme Factory ---");
        Console.WriteLine("Swapping to the Dark theme factory. UIRenderer code is UNCHANGED.\n");

        IUIFactory darkFactory = new DarkThemeFactory();
        var darkRenderer = new UIRenderer(darkFactory);

        Console.WriteLine("Rendering Login Form (Dark Theme):");
        darkRenderer.RenderLoginForm();

        Console.WriteLine("\nRendering Settings Panel (Dark Theme):");
        darkRenderer.RenderSettingsPanel();

        Console.WriteLine("\n→ Identical UIRenderer logic, completely different visual output.");
        Console.WriteLine("→ The renderer has zero knowledge of Light vs Dark — only IUIFactory matters.");

        Pause();

        // DEMONSTRATION 3: Runtime factory switching
        Console.WriteLine("--- Demonstration 3: Runtime Factory Selection ---");
        Console.WriteLine("Factory chosen at runtime based on user preference.\n");

        var userPreference = "dark"; // could come from config, database, command-line arg, etc.
        IUIFactory selectedFactory = userPreference == "dark"
            ? new DarkThemeFactory()
            : new LightThemeFactory();

        Console.WriteLine($"User preference: '{userPreference}' → using {selectedFactory.GetType().Name}\n");

        var renderer = new UIRenderer(selectedFactory);
        renderer.RenderLoginForm();

        Console.WriteLine("\n→ The selection logic is the only place that knows about concrete factory types.");

        Pause();

        // DEMONSTRATION 4: Why families matter — the consistency guarantee
        Console.WriteLine("--- Demonstration 4: Family Consistency Guarantee ---");
        Console.WriteLine("Abstract Factory enforces that ALL components come from the same family.\n");

        Console.WriteLine("With Abstract Factory:");
        Console.WriteLine("  ✓ Light factory  → only creates Light buttons + Light checkboxes");
        Console.WriteLine("  ✓ Dark factory   → only creates Dark buttons  + Dark checkboxes");
        Console.WriteLine("  ✓ Impossible to mix a LightButton with a DarkCheckbox by accident");

        Console.WriteLine("\nWithout Abstract Factory (manual creation):");
        Console.WriteLine("  ✗ var btn = new LightButton(\"OK\");           // hardcoded");
        Console.WriteLine("  ✗ var chk = new DarkCheckbox(\"Remember me\"); // inconsistent!");
        Console.WriteLine("  ✗ Nothing prevents you from mixing families");

        Console.WriteLine("\nCompare to Factory Method (1.2):");
        Console.WriteLine("  Factory Method  → one factory method, creates ONE product type");
        Console.WriteLine("  Abstract Factory → multiple factory methods, creates a FAMILY of products");

        Pause();

        // DEMONSTRATION 5: Adding a new theme (extensibility)
        Console.WriteLine("--- Demonstration 5: Extensibility ---");
        Console.WriteLine("How easy is it to add a 'High Contrast' theme?\n");

        Console.WriteLine("Steps required:");
        Console.WriteLine("  1. Create HighContrastButton : IButton");
        Console.WriteLine("  2. Create HighContrastCheckbox : ICheckbox");
        Console.WriteLine("  3. Create HighContrastFactory : IUIFactory");
        Console.WriteLine("     → CreateButton() returns new HighContrastButton(label)");
        Console.WriteLine("     → CreateCheckbox() returns new HighContrastCheckbox(label)");
        Console.WriteLine("  4. Pass the new factory to UIRenderer");
        Console.WriteLine("\n  ✓ Zero changes to UIRenderer, IUIFactory, or any existing factory");
        Console.WriteLine("  ✓ Open/Closed Principle — open for extension, closed for modification");

        Pause();

        // SUMMARY
        Console.WriteLine("=== KEY TAKEAWAYS ===");
        Console.WriteLine("✓ Abstract Factory creates FAMILIES of related objects");
        Console.WriteLine("✓ Client depends only on interfaces — never on concrete types");
        Console.WriteLine("✓ Swapping factories re-themes/re-configures an entire subsystem");
        Console.WriteLine("✓ Family consistency is guaranteed — you cannot mix products from different factories");
        Console.WriteLine("✓ Follows Open/Closed Principle — new families = new factory class, no existing changes");

        Console.WriteLine("\n=== REAL-WORLD EXAMPLES ===");
        Console.WriteLine("• UI themes (Light/Dark/HighContrast) — as shown");
        Console.WriteLine("• Cross-platform UI (Windows controls vs Mac controls vs Web controls)");
        Console.WriteLine("• Cloud provider SDKs (AWS vs Azure vs GCP — same interface, different services)");
        Console.WriteLine("• Database providers (SQL Server vs PostgreSQL vs SQLite connections/commands)");
        Console.WriteLine("• Test doubles (real infrastructure factory vs in-memory/mock factory)");

        Console.WriteLine("\nDemo complete.");
    }

    static void Pause()
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey(intercept: true);
        Console.WriteLine();
    }
}
