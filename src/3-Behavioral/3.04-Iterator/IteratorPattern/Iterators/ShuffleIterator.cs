namespace IteratorPattern;

public sealed class ShuffleIterator : IPlaylistIterator
{
    private readonly List<Song> _shuffled;
    private int _index;

    public ShuffleIterator(IReadOnlyList<Song> songs, int? seed = null)
    {
        _shuffled = [.. songs];
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();

        // Fisher-Yates shuffle — O(n), unbiased
        for (int i = _shuffled.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (_shuffled[i], _shuffled[j]) = (_shuffled[j], _shuffled[i]);
        }
    }

    public bool HasNext => _index < _shuffled.Count;

    public Song Next()
    {
        if (!HasNext)
            throw new InvalidOperationException("No more songs in the iterator.");
        return _shuffled[_index++];
    }

    public void Reset() => _index = 0;
}
