namespace ReadModelPattern.Infrastructure;

public interface IReadModelStore<TKey, TView> where TKey : notnull
{
    TView? Get(TKey key);
    void Upsert(TKey key, TView view);
    IReadOnlyList<TView> GetAll();
    void Clear();
    int Count { get; }
}
