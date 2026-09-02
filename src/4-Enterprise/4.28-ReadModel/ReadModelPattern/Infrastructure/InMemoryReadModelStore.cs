namespace ReadModelPattern.Infrastructure;

public sealed class InMemoryReadModelStore<TKey, TView> : IReadModelStore<TKey, TView>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TView> _store = new();

    public TView? Get(TKey key) => _store.TryGetValue(key, out var v) ? v : default;
    public void Upsert(TKey key, TView view) => _store[key] = view;
    public IReadOnlyList<TView> GetAll() => [.._store.Values];
    public void Clear() => _store.Clear();
    public int Count => _store.Count;
}
