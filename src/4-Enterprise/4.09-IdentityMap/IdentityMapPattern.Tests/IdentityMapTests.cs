using Microsoft.Data.Sqlite;
using IdentityMapPattern.Domain;
using IdentityMapPattern.Infrastructure;
using IdentityMapPattern.Mappers;

namespace IdentityMapPattern.Tests;

public sealed class IdentityMapTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ArtworkMapper _artworks;
    private readonly ArtistMapper _artists;

    public IdentityMapTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        Schema.Create(_connection);
        _artworks = new ArtworkMapper(_connection);
        _artists  = new ArtistMapper(_connection);
    }

    public void Dispose() => _connection.Dispose();

    // ── IdentityMap<TKey, TEntity> unit tests (no DB) ─────────────────────────

    [Fact]
    public void TryGet_ReturnsFalse_WhenKeyNotRegistered()
    {
        var map = new IdentityMap<int, string>();

        var found = map.TryGet(1, out var value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void TryGet_ReturnsTrue_AndEntity_WhenRegistered()
    {
        var map = new IdentityMap<int, string>();
        map.Register(42, "hello");

        var found = map.TryGet(42, out var value);

        Assert.True(found);
        Assert.Equal("hello", value);
    }

    [Fact]
    public void Register_OverwritesExistingEntry()
    {
        var map = new IdentityMap<int, string>();
        map.Register(1, "first");
        map.Register(1, "second");

        map.TryGet(1, out var value);

        Assert.Equal("second", value);
    }

    [Fact]
    public void Remove_DeletesEntry()
    {
        var map = new IdentityMap<int, string>();
        map.Register(7, "to be removed");
        map.Remove(7);

        Assert.False(map.Contains(7));
        Assert.Equal(0, map.Count);
    }

    [Fact]
    public void Contains_ReturnsTrueForRegisteredKey()
    {
        var map = new IdentityMap<string, int>();
        map.Register("key", 99);

        Assert.True(map.Contains("key"));
        Assert.False(map.Contains("other"));
    }

    [Fact]
    public void Count_TracksRegisteredEntries()
    {
        var map = new IdentityMap<int, string>();
        Assert.Equal(0, map.Count);

        map.Register(1, "a");
        map.Register(2, "b");
        Assert.Equal(2, map.Count);

        map.Remove(1);
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var map = new IdentityMap<int, string>();
        map.Register(1, "a");
        map.Register(2, "b");
        map.Register(3, "c");

        map.Clear();

        Assert.Equal(0, map.Count);
        Assert.False(map.Contains(1));
    }

    // ── ArtworkMapper integration tests ───────────────────────────────────────

    private Artist InsertArtist() =>
        _artists.Insert(new Artist(0, "Emily Carr", "Canadian", 1871));

    private Artwork InsertArtwork(int artistId) =>
        _artworks.Insert(new Artwork(0, "Big Raven", artistId, "Oil on canvas", 1931, 485_000m));

    [Fact]
    public void FindById_ReturnsNull_WhenNotFound()
    {
        var result = _artworks.FindById(9999);

        Assert.Null(result);
    }

    [Fact]
    public void FindById_LoadsFromDatabase_FirstCall()
    {
        var artist  = InsertArtist();
        var artwork = InsertArtwork(artist.Id);
        var mapper  = new ArtworkMapper(_connection);  // fresh — empty cache

        var result = mapper.FindById(artwork.Id);

        Assert.NotNull(result);
        Assert.Equal("Big Raven", result.Title);
        Assert.Equal(1, mapper.LoadCount);
    }

    [Fact]
    public void FindById_ReturnsSameInstance_SecondCall()
    {
        var artist  = InsertArtist();
        var artwork = InsertArtwork(artist.Id);
        var mapper  = new ArtworkMapper(_connection);

        var first  = mapper.FindById(artwork.Id);
        var second = mapper.FindById(artwork.Id);

        Assert.Same(first, second);
        Assert.Equal(1, mapper.LoadCount);  // only one DB hit
    }

    [Fact]
    public void FindById_ReferenceEquals_True_ForSameId()
    {
        var artist  = InsertArtist();
        var artwork = InsertArtwork(artist.Id);
        var mapper  = new ArtworkMapper(_connection);

        var a = mapper.FindById(artwork.Id)!;
        var b = mapper.FindById(artwork.Id)!;

        Assert.True(ReferenceEquals(a, b));
    }

    [Fact]
    public void Insert_AssignsId_AndRegistersInMap()
    {
        var artist = InsertArtist();
        var artwork = new Artwork(0, "Forest, BC", artist.Id, "Oil on canvas", 1932, 320_000m);

        var saved = _artworks.Insert(artwork);

        Assert.True(saved.Id > 0);
        Assert.Equal(0, _artworks.LoadCount);     // INSERT, not a DB load
        Assert.True(_artworks.CacheSize >= 1);
    }

    [Fact]
    public void FindAll_PopulatesMap_ForAllArtworks()
    {
        var artist = InsertArtist();
        var mapper = new ArtworkMapper(_connection);
        mapper.Insert(new Artwork(0, "A", artist.Id, "Oil", 1930, 100_000m));
        mapper.Insert(new Artwork(0, "B", artist.Id, "Oil", 1931, 200_000m));
        mapper.Insert(new Artwork(0, "C", artist.Id, "Oil", 1932, 300_000m));

        var freshMapper = new ArtworkMapper(_connection);
        var all = freshMapper.FindAll();

        Assert.Equal(3, all.Count);
        Assert.Equal(3, freshMapper.CacheSize);
    }

    [Fact]
    public void FindAll_ThenFindById_ReturnsSameInstance()
    {
        var artist  = InsertArtist();
        var artwork = InsertArtwork(artist.Id);
        var mapper  = new ArtworkMapper(_connection);

        var all        = mapper.FindAll();
        var loadAfterAll = mapper.LoadCount;
        var byId       = mapper.FindById(artwork.Id);

        Assert.Equal(loadAfterAll, mapper.LoadCount);  // no extra DB hit
        Assert.Same(all.Single(), byId);
    }

    [Fact]
    public void Update_PersistsChange_AndRefreshesMap()
    {
        var artist  = InsertArtist();
        var artwork = InsertArtwork(artist.Id);
        var mapper  = new ArtworkMapper(_connection);

        var loaded = mapper.FindById(artwork.Id)!;
        loaded.PutOnDisplay();
        mapper.Update(loaded);

        var reloaded = new ArtworkMapper(_connection).FindById(artwork.Id)!;
        Assert.True(reloaded.OnDisplay);
    }

    [Fact]
    public void Delete_RemovesFromDb_AndEvictsFromMap()
    {
        var artist  = InsertArtist();
        var artwork = InsertArtwork(artist.Id);
        var mapper  = new ArtworkMapper(_connection);
        mapper.FindById(artwork.Id);  // populate cache

        mapper.Delete(artwork.Id);

        Assert.False(mapper.CacheSize > 0 && mapper.CacheSize == 1);
        Assert.Null(mapper.FindById(artwork.Id));
    }

    [Fact]
    public void FindByArtist_ReturnsMatchingArtworks()
    {
        var carr    = InsertArtist();
        var harris  = _artists.Insert(new Artist(0, "Lawren Harris", "Canadian", 1885));
        var mapper  = new ArtworkMapper(_connection);
        mapper.Insert(new Artwork(0, "Big Raven",    carr.Id,   "Oil", 1931, 485_000m));
        mapper.Insert(new Artwork(0, "Forest, BC",   carr.Id,   "Oil", 1932, 320_000m));
        mapper.Insert(new Artwork(0, "Lake Superior", harris.Id, "Oil", 1924, 1_200_000m));

        var carrArtworks = mapper.FindByArtist(carr.Id);

        Assert.Equal(2, carrArtworks.Count);
        Assert.All(carrArtworks, a => Assert.Equal(carr.Id, a.ArtistId));
    }

    // ── ArtistMapper integration tests ────────────────────────────────────────

    [Fact]
    public void ArtistMapper_FindById_ReturnsSameInstance_SecondCall()
    {
        var inserted = _artists.Insert(new Artist(0, "Lawren Harris", "Canadian", 1885));
        var mapper   = new ArtistMapper(_connection);

        var first  = mapper.FindById(inserted.Id);
        var second = mapper.FindById(inserted.Id);

        Assert.Same(first, second);
        Assert.Equal(1, mapper.LoadCount);
    }

    [Fact]
    public void ArtistMapper_Insert_AssignsId()
    {
        var artist = new Artist(0, "Alex Colville", "Canadian", 1920);

        var saved = _artists.Insert(artist);

        Assert.True(saved.Id > 0);
        Assert.Equal("Alex Colville", saved.Name);
    }

    [Fact]
    public void ArtistMapper_FindAll_ReturnsAllArtists()
    {
        var mapper = new ArtistMapper(_connection);
        mapper.Insert(new Artist(0, "Emily Carr",    "Canadian", 1871));
        mapper.Insert(new Artist(0, "Lawren Harris", "Canadian", 1885));
        mapper.Insert(new Artist(0, "Alex Colville", "Canadian", 1920));

        var all = mapper.FindAll();

        Assert.Equal(3, all.Count);
        Assert.Equal(3, mapper.CacheSize);
    }

    [Fact]
    public void ArtistMapper_Update_PersistsRename_AndRefreshesMap()
    {
        var artist = _artists.Insert(new Artist(0, "Jean-Pual Riopelle", "Canadian", 1923));
        var mapper = new ArtistMapper(_connection);

        var loaded = mapper.FindById(artist.Id)!;
        loaded.Rename("Jean-Paul Riopelle");
        mapper.Update(loaded);

        var reloaded = new ArtistMapper(_connection).FindById(artist.Id)!;
        Assert.Equal("Jean-Paul Riopelle", reloaded.Name);
    }

    [Fact]
    public void Artist_Rename_Throws_WhenNameIsBlank()
    {
        var artist = _artists.Insert(new Artist(0, "Emily Carr", "Canadian", 1871));

        Assert.Throws<ArgumentException>(() => artist.Rename(""));
        Assert.Throws<ArgumentException>(() => artist.Rename("   "));
    }
}
