namespace IteratorPattern;

public interface IPlaylistIterator
{
    bool HasNext { get; }
    Song Next();
    void Reset();
}
