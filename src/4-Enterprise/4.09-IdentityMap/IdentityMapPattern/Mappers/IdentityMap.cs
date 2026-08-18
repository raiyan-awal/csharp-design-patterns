namespace IdentityMapPattern.Mappers;

public sealed class IdentityMap<TKey, TEntity> where TKey : notnull
{
    private readonly Dictionary<TKey, TEntity> _store = new();

    public bool TryGet(TKey key, out TEntity? entity) => _store.TryGetValue(key, out entity);
    public void Register(TKey key, TEntity entity) => _store[key] = entity;
    public void Remove(TKey key) => _store.Remove(key);
    public bool Contains(TKey key) => _store.ContainsKey(key);
    public int Count => _store.Count;
    public void Clear() => _store.Clear();
}
