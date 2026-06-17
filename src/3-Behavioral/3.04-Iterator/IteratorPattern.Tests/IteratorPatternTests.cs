using IteratorPattern;

namespace IteratorPattern.Tests;

file static class Songs
{
    public static Song Rock1  => new("Song A", "Artist 1", "Rock",    200);
    public static Song Rock2  => new("Song B", "Artist 2", "Rock",    250);
    public static Song Pop1   => new("Song C", "Artist 3", "Pop",     180);
    public static Song Pop2   => new("Song D", "Artist 4", "Pop",     210);
    public static Song HipHop => new("Song E", "Artist 5", "Hip-Hop", 300);
}

// ── SequentialIterator ────────────────────────────────────────

public class SequentialIteratorTests
{
    private static IReadOnlyList<Song> ThreeSongs => [Songs.Rock1, Songs.Pop1, Songs.HipHop];

    [Fact]
    public void HasNext_True_WhenSongsRemain()
    {
        var iter = new SequentialIterator(ThreeSongs);
        Assert.True(iter.HasNext);
    }

    [Fact]
    public void HasNext_False_WhenAllConsumed()
    {
        var iter = new SequentialIterator(ThreeSongs);
        while (iter.HasNext) iter.Next();
        Assert.False(iter.HasNext);
    }

    [Fact]
    public void HasNext_False_OnEmptyList()
    {
        var iter = new SequentialIterator([]);
        Assert.False(iter.HasNext);
    }

    [Fact]
    public void Next_ReturnsInOriginalOrder()
    {
        var songs = ThreeSongs;
        var iter  = new SequentialIterator(songs);
        Assert.Equal(songs[0], iter.Next());
        Assert.Equal(songs[1], iter.Next());
        Assert.Equal(songs[2], iter.Next());
    }

    [Fact]
    public void Next_WhenExhausted_ThrowsInvalidOperationException()
    {
        var iter = new SequentialIterator([Songs.Rock1]);
        iter.Next();
        Assert.Throws<InvalidOperationException>(() => iter.Next());
    }

    [Fact]
    public void Reset_AllowsReIteration()
    {
        var iter  = new SequentialIterator(ThreeSongs);
        var first = iter.Next();
        while (iter.HasNext) iter.Next();

        iter.Reset();

        Assert.True(iter.HasNext);
        Assert.Equal(first, iter.Next());
    }
}

// ── ShuffleIterator ───────────────────────────────────────────

public class ShuffleIteratorTests
{
    private static IReadOnlyList<Song> FiveSongs =>
        [Songs.Rock1, Songs.Rock2, Songs.Pop1, Songs.Pop2, Songs.HipHop];

    [Fact]
    public void HasNext_False_WhenAllConsumed()
    {
        var iter = new ShuffleIterator(FiveSongs, seed: 1);
        while (iter.HasNext) iter.Next();
        Assert.False(iter.HasNext);
    }

    [Fact]
    public void Next_ReturnsAllSongsExactlyOnce()
    {
        var songs    = FiveSongs;
        var iter     = new ShuffleIterator(songs, seed: 99);
        var consumed = new List<Song>();
        while (iter.HasNext) consumed.Add(iter.Next());

        Assert.Equal(songs.Count, consumed.Count);
        foreach (var s in songs)
            Assert.Contains(s, consumed);
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentOrders()
    {
        var songs = FiveSongs;
        var order1 = Drain(new ShuffleIterator(songs, seed: 1));
        var order2 = Drain(new ShuffleIterator(songs, seed: 2));
        Assert.False(order1.SequenceEqual(order2));
    }

    [Fact]
    public void SameSeed_ProducesSameOrder()
    {
        var songs = FiveSongs;
        var order1 = Drain(new ShuffleIterator(songs, seed: 42));
        var order2 = Drain(new ShuffleIterator(songs, seed: 42));
        Assert.True(order1.SequenceEqual(order2));
    }

    [Fact]
    public void Reset_AllowsReIteration()
    {
        var iter = new ShuffleIterator(FiveSongs, seed: 7);
        var first = iter.Next();
        while (iter.HasNext) iter.Next();

        iter.Reset();

        Assert.True(iter.HasNext);
        Assert.Equal(first, iter.Next());
    }

    private static List<Song> Drain(IPlaylistIterator iter)
    {
        var result = new List<Song>();
        while (iter.HasNext) result.Add(iter.Next());
        return result;
    }
}

// ── FilterIterator ────────────────────────────────────────────

public class FilterIteratorTests
{
    private static IReadOnlyList<Song> MixedSongs =>
        [Songs.Rock1, Songs.Pop1, Songs.Rock2, Songs.Pop2, Songs.HipHop];

    [Fact]
    public void Next_ReturnsOnlyMatchingSongs()
    {
        var iter   = new FilterIterator(MixedSongs, s => s.Genre == "Rock");
        var result = new List<Song>();
        while (iter.HasNext) result.Add(iter.Next());

        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal("Rock", s.Genre));
    }

    [Fact]
    public void HasNext_False_WhenNoSongsMatch()
    {
        var iter = new FilterIterator(MixedSongs, s => s.Genre == "Jazz");
        Assert.False(iter.HasNext);
    }

    [Fact]
    public void Next_WhenExhausted_ThrowsInvalidOperationException()
    {
        var iter = new FilterIterator(MixedSongs, s => s.Genre == "Jazz");
        Assert.Throws<InvalidOperationException>(() => iter.Next());
    }

    [Fact]
    public void Reset_AllowsReIteration()
    {
        var iter  = new FilterIterator(MixedSongs, s => s.Genre == "Pop");
        var first = iter.Next();
        while (iter.HasNext) iter.Next();

        iter.Reset();

        Assert.True(iter.HasNext);
        Assert.Equal(first, iter.Next());
    }

    [Fact]
    public void Filter_AlwaysTrue_ReturnsAllSongs()
    {
        var iter   = new FilterIterator(MixedSongs, _ => true);
        var result = new List<Song>();
        while (iter.HasNext) result.Add(iter.Next());
        Assert.Equal(MixedSongs.Count, result.Count);
    }
}

// ── Playlist ──────────────────────────────────────────────────

public class PlaylistTests
{
    private static Playlist BuildPlaylist()
    {
        var p = new Playlist("Test");
        p.Add(Songs.Rock1);
        p.Add(Songs.Pop1);
        p.Add(Songs.HipHop);
        return p;
    }

    [Fact]
    public void GetSequentialIterator_ReturnsAllSongsInOrder()
    {
        var p    = BuildPlaylist();
        var iter = p.GetSequentialIterator();
        Assert.Equal(Songs.Rock1,  iter.Next());
        Assert.Equal(Songs.Pop1,   iter.Next());
        Assert.Equal(Songs.HipHop, iter.Next());
        Assert.False(iter.HasNext);
    }

    [Fact]
    public void GetFilterIterator_ReturnsOnlyMatching()
    {
        var p    = BuildPlaylist();
        var iter = p.GetFilterIterator(s => s.Genre == "Rock");
        Assert.True(iter.HasNext);
        Assert.Equal(Songs.Rock1, iter.Next());
        Assert.False(iter.HasNext);
    }

    [Fact]
    public void AsEnumerable_ReturnsAllSongsInOrder()
    {
        var p      = BuildPlaylist();
        var result = p.AsEnumerable().ToList();
        Assert.Equal(3, result.Count);
        Assert.Equal(Songs.Rock1,  result[0]);
        Assert.Equal(Songs.Pop1,   result[1]);
        Assert.Equal(Songs.HipHop, result[2]);
    }

    [Fact]
    public void MultipleIterators_CanCoexistIndependently()
    {
        var p     = BuildPlaylist();
        var iter1 = p.GetSequentialIterator();
        var iter2 = p.GetSequentialIterator();

        iter1.Next();  // advance iter1

        // iter2 should still be at the start
        Assert.Equal(Songs.Rock1, iter2.Next());
    }
}
