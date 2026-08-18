using System.Data;
using Dapper;
using IdentityMapPattern.Domain;

namespace IdentityMapPattern.Mappers;

public sealed class ArtistMapper
{
    private readonly IDbConnection _db;
    private readonly IdentityMap<int, Artist> _map = new();

    public ArtistMapper(IDbConnection db) => _db = db;

    public int CacheSize => _map.Count;
    public int LoadCount { get; private set; }

    public Artist? FindById(int id)
    {
        if (_map.TryGet(id, out var cached)) return cached;

        LoadCount++;
        var row = _db.QuerySingleOrDefault<Row>(
            "SELECT * FROM Artists WHERE Id = @id", new { id });
        if (row is null) return null;

        var artist = row.ToDomain();
        _map.Register(id, artist);
        return artist;
    }

    public IReadOnlyList<Artist> FindAll()
    {
        var rows = _db.Query<Row>("SELECT * FROM Artists ORDER BY Name").ToList();
        var result = new List<Artist>(rows.Count);
        foreach (var row in rows)
        {
            if (_map.TryGet(row.Id, out var cached))
                result.Add(cached!);
            else
            {
                LoadCount++;
                var artist = row.ToDomain();
                _map.Register(row.Id, artist);
                result.Add(artist);
            }
        }
        return result;
    }

    public Artist Insert(Artist artist)
    {
        var id = _db.ExecuteScalar<int>(
            """
            INSERT INTO Artists (Name, Nationality, BirthYear)
            VALUES (@Name, @Nationality, @BirthYear);
            SELECT last_insert_rowid();
            """,
            new { artist.Name, artist.Nationality, artist.BirthYear });
        var saved = artist.WithId(id);
        _map.Register(id, saved);
        return saved;
    }

    public void Update(Artist artist)
    {
        _db.Execute(
            """
            UPDATE Artists
            SET Name = @Name, Nationality = @Nationality, BirthYear = @BirthYear
            WHERE Id = @Id
            """,
            new { artist.Name, artist.Nationality, artist.BirthYear, artist.Id });
        _map.Register(artist.Id, artist);
    }

    public void Delete(int id)
    {
        _db.Execute("DELETE FROM Artists WHERE Id = @id", new { id });
        _map.Remove(id);
    }

    private sealed class Row
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public string Nationality { get; init; } = "";
        public int BirthYear { get; init; }

        public Artist ToDomain() => new(Id, Name, Nationality, BirthYear);
    }
}
