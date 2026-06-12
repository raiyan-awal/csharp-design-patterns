namespace ProxyPattern;

public sealed class CachingDocumentProxy : IDocumentRepository
{
    private readonly IDocumentRepository _inner;
    private readonly Dictionary<string, Document?> _cache = [];

    public int CacheHitCount  { get; private set; }
    public int CacheMissCount { get; private set; }

    public CachingDocumentProxy(IDocumentRepository inner) => _inner = inner;

    public Document? Load(string id)
    {
        if (_cache.TryGetValue(id, out var cached))
        {
            CacheHitCount++;
            Console.WriteLine($"  [CACHE] Hit  for '{id}'");
            return cached;
        }

        CacheMissCount++;
        Console.WriteLine($"  [CACHE] Miss for '{id}' — forwarding to inner");
        var doc = _inner.Load(id);
        _cache[id] = doc;
        return doc;
    }

    public IReadOnlyList<string> ListIds() => _inner.ListIds();

    public void Invalidate(string id) => _cache.Remove(id);
    public void InvalidateAll()       => _cache.Clear();
}
