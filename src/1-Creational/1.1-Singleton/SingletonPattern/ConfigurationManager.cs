namespace SingletonPattern;

/// <summary>
/// SINGLETON PATTERN IMPLEMENTATION
///
/// This class demonstrates the Singleton pattern - ensuring only ONE instance
/// of ConfigurationManager exists throughout the application's lifetime.
///
/// WHY USE SINGLETON?
/// - You need exactly one instance of a class (e.g., configuration, logging, caching)
/// - You want global access to that instance
/// - You want to control instantiation (lazy initialization, thread-safety)
///
/// BENEFITS:
/// ✓ Controlled access to single instance
/// ✓ Reduced memory footprint (only one instance)
/// ✓ Global point of access
/// ✓ Thread-safe (when implemented correctly)
///
/// DRAWBACKS:
/// ✗ Can make unit testing difficult (hard to mock)
/// ✗ Violates Single Responsibility Principle (manages own creation + business logic)
/// ✗ Can hide dependencies (not obvious from constructor)
/// ✗ Global state can lead to coupling
///
/// WHEN TO USE:
/// - Configuration managers
/// - Logger instances
/// - Database connection pools
/// - Caches
/// - Thread pools
///
/// WHEN NOT TO USE:
/// - When you need multiple instances with different configurations
/// - In unit tests (prefer dependency injection instead)
/// - When state needs to be reset between operations
/// </summary>
public sealed class ConfigurationManager
{
    // Private static instance - holds the single instance of this class
    // Lazy<T> ensures thread-safe lazy initialization (instance created only when first accessed)
    // LazyThreadSafetyMode.ExecutionAndPublication ensures only one thread creates the instance
    private static readonly Lazy<ConfigurationManager> _instance =
        new Lazy<ConfigurationManager>(() => new ConfigurationManager());

    // Dictionary to store configuration settings (key-value pairs)
    private readonly Dictionary<string, string> _settings;

    /// <summary>
    /// PRIVATE CONSTRUCTOR - This is the key to Singleton pattern!
    ///
    /// By making the constructor private, we prevent external code from creating
    /// new instances using 'new ConfigurationManager()'.
    ///
    /// The only way to get an instance is through the Instance property.
    /// </summary>
    private ConfigurationManager()
    {
        Console.WriteLine("[Singleton] ConfigurationManager instance created (this should only appear ONCE)");

        // Initialize with some default configuration settings
        _settings = new Dictionary<string, string>
        {
            { "AppName", "Design Patterns Demo" },
            { "Version", "1.0.0" },
            { "Environment", "Development" }
        };
    }

    /// <summary>
    /// PUBLIC STATIC PROPERTY - The global access point to the singleton instance.
    ///
    /// This is how external code gets the single instance:
    ///     var config = ConfigurationManager.Instance;
    ///
    /// The Lazy<T> wrapper ensures:
    /// 1. Instance is only created when first accessed (lazy initialization)
    /// 2. Thread-safe creation (no race conditions in multi-threaded scenarios)
    /// 3. Instance is created only once, even if multiple threads call this simultaneously
    /// </summary>
    public static ConfigurationManager Instance => _instance.Value;

    /// <summary>
    /// Get a configuration value by key.
    /// Returns null if the key doesn't exist.
    /// </summary>
    public string? GetSetting(string key)
    {
        return _settings.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Set or update a configuration value.
    /// If the key exists, its value is updated. Otherwise, a new key-value pair is added.
    /// </summary>
    public void SetSetting(string key, string value)
    {
        _settings[key] = value;
        Console.WriteLine($"[Singleton] Setting updated: {key} = {value}");
    }

    /// <summary>
    /// Get all configuration settings.
    /// Returns a read-only dictionary to prevent external modification.
    /// </summary>
    public IReadOnlyDictionary<string, string> GetAllSettings()
    {
        return _settings;
    }

    /// <summary>
    /// Display all current configuration settings.
    /// Useful for debugging and demonstration purposes.
    /// </summary>
    public void DisplaySettings()
    {
        Console.WriteLine("\n[Singleton] Current Configuration Settings:");
        foreach (var setting in _settings)
        {
            Console.WriteLine($"  {setting.Key} = {setting.Value}");
        }
    }
}

/// <summary>
/// ALTERNATIVE SINGLETON IMPLEMENTATION: Thread-Safe without Lazy<T>
///
/// This is an older approach using static initialization.
/// It's simpler but doesn't provide lazy initialization - the instance
/// is created when the class is first referenced, not when first accessed.
/// </summary>
public sealed class EagerSingleton
{
    // Instance is created immediately when the class is loaded
    // This is thread-safe because static constructors are guaranteed to run only once
    private static readonly EagerSingleton _instance = new EagerSingleton();

    // Private constructor prevents external instantiation
    private EagerSingleton()
    {
        Console.WriteLine("[EagerSingleton] Instance created at class load time");
    }

    // Public property to access the singleton instance
    public static EagerSingleton Instance => _instance;

    public string GetMessage() => "I'm an eager singleton - created immediately!";
}

/// <summary>
/// ANTI-PATTERN: NOT Thread-Safe Singleton (DO NOT USE IN PRODUCTION)
///
/// This implementation is NOT thread-safe and can create multiple instances
/// in multi-threaded environments. Included here for educational purposes only.
/// </summary>
public sealed class UnsafeSingleton
{
    private static UnsafeSingleton? _instance;

    private UnsafeSingleton() { }

    // PROBLEM: In a multi-threaded environment, multiple threads can pass
    // the null check simultaneously and create multiple instances!
    public static UnsafeSingleton Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new UnsafeSingleton(); // ⚠️ Race condition here!
            }
            return _instance;
        }
    }
}
