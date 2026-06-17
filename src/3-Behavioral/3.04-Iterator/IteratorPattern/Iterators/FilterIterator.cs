namespace IteratorPattern;

public sealed class FilterIterator : IPlaylistIterator
{
    private readonly List<Song> _filtered;
    private int _index;

    public FilterIterator(IReadOnlyList<Song> songs, Func<Song, bool> predicate)
        => _filtered = songs.Where(predicate).ToList();

    public bool HasNext => _index < _filtered.Count;

    public Song Next()
    {
        if (!HasNext)
            throw new InvalidOperationException("No more songs in the iterator.");
        return _filtered[_index++];
    }

    public void Reset() => _index = 0;
}
