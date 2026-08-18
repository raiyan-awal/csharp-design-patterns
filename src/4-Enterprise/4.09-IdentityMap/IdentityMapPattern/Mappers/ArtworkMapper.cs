using System.Data;
using Dapper;
using IdentityMapPattern.Domain;

namespace IdentityMapPattern.Mappers;

public sealed class ArtworkMapper
{
    private readonly IDbConnection _db;
    private readonly IdentityMap<int, Artwork> _map = new();

    public ArtworkMapper(IDbConnection db) => _db = db;

    public int CacheSize => _map.Count;
    public int LoadCount { get; private set; }

    public Artwork? FindById(int id)
    {
        if (_map.TryGet(id, out var cached)) return cached;

        LoadCount++;
        var row = _db.QuerySingleOrDefault<Row>(
            "SELECT * FROM Artworks WHERE Id = @id", new { id });
        if (row is null) return null;

        var artwork = row.ToDomain();
        _map.Register(id, artwork);
        return artwork;
    }

    public IReadOnlyList<Artwork> FindAll()
    {
        var rows = _db.Query<Row>("SELECT * FROM Artworks ORDER BY Title").ToList();
        var result = new List<Artwork>(rows.Count);
        foreach (var row in rows)
        {
            if (_map.TryGet(row.Id, out var cached))
                result.Add(cached!);
            else
            {
                LoadCount++;
                var artwork = row.ToDomain();
                _map.Register(row.Id, artwork);
                result.Add(artwork);
            }
        }
        return result;
    }

    public IReadOnlyList<Artwork> FindByArtist(int artistId)
    {
        var rows = _db.Query<Row>(
            "SELECT * FROM Artworks WHERE ArtistId = @artistId ORDER BY Year",
            new { artistId }).ToList();
        var result = new List<Artwork>(rows.Count);
        foreach (var row in rows)
        {
            if (_map.TryGet(row.Id, out var cached))
                result.Add(cached!);
            else
            {
                LoadCount++;
                var artwork = row.ToDomain();
                _map.Register(row.Id, artwork);
                result.Add(artwork);
            }
        }
        return result;
    }

    public Artwork Insert(Artwork artwork)
    {
        var id = _db.ExecuteScalar<int>(
            """
            INSERT INTO Artworks (Title, ArtistId, Medium, Year, ValuationCad, OnDisplay)
            VALUES (@Title, @ArtistId, @Medium, @Year, @ValuationCad, @OnDisplay);
            SELECT last_insert_rowid();
            """,
            new
            {
                artwork.Title,
                artwork.ArtistId,
                artwork.Medium,
                artwork.Year,
                ValuationCad = artwork.ValuationCad.ToString("F2"),
                OnDisplay = artwork.OnDisplay ? 1 : 0
            });
        var saved = artwork.WithId(id);
        _map.Register(id, saved);
        return saved;
    }

    public void Update(Artwork artwork)
    {
        _db.Execute(
            """
            UPDATE Artworks
            SET Title = @Title, ArtistId = @ArtistId, Medium = @Medium,
                Year = @Year, ValuationCad = @ValuationCad, OnDisplay = @OnDisplay
            WHERE Id = @Id
            """,
            new
            {
                artwork.Title,
                artwork.ArtistId,
                artwork.Medium,
                artwork.Year,
                ValuationCad = artwork.ValuationCad.ToString("F2"),
                OnDisplay = artwork.OnDisplay ? 1 : 0,
                artwork.Id
            });
        _map.Register(artwork.Id, artwork);
    }

    public void Delete(int id)
    {
        _db.Execute("DELETE FROM Artworks WHERE Id = @id", new { id });
        _map.Remove(id);
    }

    private sealed class Row
    {
        public int Id { get; init; }
        public string Title { get; init; } = "";
        public int ArtistId { get; init; }
        public string Medium { get; init; } = "";
        public int Year { get; init; }
        public string ValuationCad { get; init; } = "";
        public int OnDisplay { get; init; }

        public Artwork ToDomain() =>
            new(Id, Title, ArtistId, Medium, Year, decimal.Parse(ValuationCad), OnDisplay != 0);
    }
}
