namespace IteratorPattern;

public sealed class Playlist
{
    private readonly List<Song> _songs = [];

    public string Name { get; }
    public int Count  => _songs.Count;

    public Playlist(string name) => Name = name;

    public void Add(Song song) => _songs.Add(song);

    public IReadOnlyList<Song> Songs => _songs;

    public IPlaylistIterator GetSequentialIterator()
        => new SequentialIterator(_songs);

    public IPlaylistIterator GetShuffleIterator(int? seed = null)
        => new ShuffleIterator(_songs, seed);

    public IPlaylistIterator GetFilterIterator(Func<Song, bool> predicate)
        => new FilterIterator(_songs, predicate);

    // C# native: yield return compiles to the same state machine as a manual IEnumerator
    public IEnumerable<Song> AsEnumerable()
    {
        foreach (var song in _songs)
            yield return song;
    }
}
