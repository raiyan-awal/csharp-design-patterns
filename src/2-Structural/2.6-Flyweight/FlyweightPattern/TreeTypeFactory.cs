namespace FlyweightPattern;

// ============================================================
// FLYWEIGHT FACTORY
// ============================================================
// Returns a cached TreeType for each unique combination of
// (species, colour, texture). Creates one on the first request;
// returns the same instance on every subsequent matching request.
//
// The factory makes sharing transparent — callers just describe
// the type they want; deduplication is handled automatically.

public sealed class TreeTypeFactory
{
    private readonly Dictionary<string, TreeType> _cache = [];

    public TreeType GetOrCreate(string species, string colour, string texture)
    {
        string key = $"{species}|{colour}|{texture}";

        if (!_cache.TryGetValue(key, out var type))
        {
            type = new TreeType(species, colour, texture);
            _cache[key] = type;
            Console.WriteLine($"  [FACTORY] Created new TreeType: {species}");
        }

        return type;
    }

    public int UniqueTypeCount => _cache.Count;

    public IReadOnlyCollection<TreeType> CachedTypes => _cache.Values;
}
