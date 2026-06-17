namespace IteratorPattern;

public sealed class SequentialIterator : IPlaylistIterator
{
    private readonly IReadOnlyList<Song> _songs;
    private int _index;

    public SequentialIterator(IReadOnlyList<Song> songs) => _songs = songs;

    public bool HasNext => _index < _songs.Count;

    public Song Next()
    {
        if (!HasNext)
            throw new InvalidOperationException("No more songs in the iterator.");
        return _songs[_index++];
    }

    public void Reset() => _index = 0;
}
