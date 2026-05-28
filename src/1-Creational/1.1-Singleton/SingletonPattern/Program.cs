namespace SingletonPattern;

/// <summary>
/// SINGLETON PATTERN DEMONSTRATION
///
/// This program demonstrates the Singleton pattern by showing:
/// 1. Only one instance is created, no matter how many times we access it
/// 2. All references point to the same instance (same memory address)
/// 3. State is shared across all accesses
/// 4. Thread-safe access in multi-threaded scenarios
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== SINGLETON PATTERN DEMO ===");
        Console.WriteLine("Press any key after each section to continue.\n");

        // DEMONSTRATION 1: Single Instance Creation
        Console.WriteLine("--- Demonstration 1: Single Instance ---");
        Console.WriteLine("Accessing ConfigurationManager for the first time...");

        var config1 = ConfigurationManager.Instance;
        config1.DisplaySettings();

        Console.WriteLine("\nAccessing ConfigurationManager for the second time...");

        var config2 = ConfigurationManager.Instance;
        config2.DisplaySettings();

        Pause();

        // DEMONSTRATION 2: Same Instance Reference
        Console.WriteLine("--- Demonstration 2: Same Instance Reference ---");
        Console.WriteLine($"config1 and config2 are the same instance: {ReferenceEquals(config1, config2)}");
        Console.WriteLine("(ReferenceEquals checks if both variables point to the exact same object in memory)");

        Pause();

        // DEMONSTRATION 3: Shared State
        Console.WriteLine("--- Demonstration 3: Shared State ---");
        Console.WriteLine("Modifying settings through config1...");

        config1.SetSetting("DatabaseConnection", "Server=localhost;Database=TestDB");
        config1.SetSetting("LogLevel", "Debug");

        Console.WriteLine("\nReading settings through config2 (different variable, same instance):");
        Console.WriteLine($"DatabaseConnection: {config2.GetSetting("DatabaseConnection")}");
        Console.WriteLine($"LogLevel: {config2.GetSetting("LogLevel")}");
        Console.WriteLine("\n→ Changes made via config1 are immediately visible via config2 because they ARE the same object.");

        Pause();

        // DEMONSTRATION 4: Multi-Threaded Access
        Console.WriteLine("--- Demonstration 4: Thread-Safe Access ---");
        Console.WriteLine("Creating 5 threads that all access the singleton simultaneously...\n");

        var threads = new List<Thread>();

        for (int i = 1; i <= 5; i++)
        {
            int threadId = i;
            var thread = new Thread(() =>
            {
                var config = ConfigurationManager.Instance;
                config.SetSetting($"Thread{threadId}", $"Processed by thread {threadId}");
                Thread.Sleep(100);
            });

            threads.Add(thread);
            thread.Start();
        }

        foreach (var thread in threads)
            thread.Join();

        Console.WriteLine("\nAll threads completed. Displaying final configuration:");
        ConfigurationManager.Instance.DisplaySettings();
        Console.WriteLine("\n→ Only one instance was created, even under concurrent access.");

        Pause();

        // DEMONSTRATION 5: Eager Singleton
        Console.WriteLine("--- Demonstration 5: Eager Singleton (Alternative) ---");
        Console.WriteLine("The EagerSingleton is created at class load time, not on first access.\n");

        var eager1 = EagerSingleton.Instance;
        var eager2 = EagerSingleton.Instance;

        Console.WriteLine($"Message: {eager1.GetMessage()}");
        Console.WriteLine($"eager1 and eager2 are the same: {ReferenceEquals(eager1, eager2)}");
        Console.WriteLine("\n→ Simpler than Lazy<T>, but the instance is always created even if never used.");

        Pause();

        // SUMMARY
        Console.WriteLine("=== KEY TAKEAWAYS ===");
        Console.WriteLine("✓ Only ONE instance is created ('instance created' appeared only once above)");
        Console.WriteLine("✓ All variables point to the SAME instance in memory");
        Console.WriteLine("✓ State is SHARED across all accesses");
        Console.WriteLine("✓ Thread-safe — multiple threads can safely access the singleton");
        Console.WriteLine("✓ Lazy<T> = instance created on first access; static field = created at class load");

        Console.WriteLine("\n=== WHEN TO USE ===");
        Console.WriteLine("• Configuration managers (like this example)");
        Console.WriteLine("• Logger instances");
        Console.WriteLine("• Database connection pools");
        Console.WriteLine("• Caches");
        Console.WriteLine("• Hardware interface access (printer spooler, etc.)");

        Console.WriteLine("\n=== WHEN NOT TO USE ===");
        Console.WriteLine("• When you need different configurations in different contexts");
        Console.WriteLine("• In unit tests (hard to mock — prefer dependency injection)");
        Console.WriteLine("• When state needs to be reset between operations");
        Console.WriteLine("• When the class has mutable state that changes based on input");

        Console.WriteLine("\nDemo complete.");
    }

    static void Pause()
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey(intercept: true);
        Console.WriteLine();
    }
}
