using System.Data;
using Dapper;
using DataMapperPattern.Domain;

namespace DataMapperPattern.Mappers;

public sealed class FilmMapper(IDbConnection db) : IMapper<Film>
{
    // Private DTO — Dapper fills this; the domain Film stays unaware of column names.
    private sealed class FilmRow
    {
        public int Id { get; init; }
        public string Title { get; init; } = "";
        public string Director { get; init; } = "";
        public string Genre { get; init; } = "";
        public int ReleaseYear { get; init; }
        public int RuntimeMinutes { get; init; }
        public bool CertifiedFresh { get; init; }

        public Film ToDomain() =>
            new(Id, Title, Director, Genre, ReleaseYear, RuntimeMinutes, CertifiedFresh);
    }

    public Film? FindById(int id)
    {
        var row = db.QuerySingleOrDefault<FilmRow>(
            "SELECT Id, Title, Director, Genre, ReleaseYear, RuntimeMinutes, CertifiedFresh " +
            "FROM Films WHERE Id = @id", new { id });
        return row?.ToDomain();
    }

    public IReadOnlyList<Film> FindAll()
    {
        var rows = db.Query<FilmRow>(
            "SELECT Id, Title, Director, Genre, ReleaseYear, RuntimeMinutes, CertifiedFresh " +
            "FROM Films ORDER BY ReleaseYear");
        return rows.Select(r => r.ToDomain()).ToList().AsReadOnly();
    }

    public IReadOnlyList<Film> FindByGenre(string genre)
    {
        var rows = db.Query<FilmRow>(
            "SELECT Id, Title, Director, Genre, ReleaseYear, RuntimeMinutes, CertifiedFresh " +
            "FROM Films WHERE Genre = @genre ORDER BY ReleaseYear", new { genre });
        return rows.Select(r => r.ToDomain()).ToList().AsReadOnly();
    }

    public IReadOnlyList<Film> FindByDirector(string director)
    {
        var rows = db.Query<FilmRow>(
            "SELECT Id, Title, Director, Genre, ReleaseYear, RuntimeMinutes, CertifiedFresh " +
            "FROM Films WHERE Director = @director ORDER BY ReleaseYear", new { director });
        return rows.Select(r => r.ToDomain()).ToList().AsReadOnly();
    }

    public Film Insert(Film film)
    {
        var id = db.ExecuteScalar<int>(
            "INSERT INTO Films (Title, Director, Genre, ReleaseYear, RuntimeMinutes, CertifiedFresh) " +
            "VALUES (@Title, @Director, @Genre, @ReleaseYear, @RuntimeMinutes, @CertifiedFresh); " +
            "SELECT last_insert_rowid();",
            new
            {
                film.Title, film.Director, film.Genre,
                film.ReleaseYear, film.RuntimeMinutes, film.CertifiedFresh
            });
        return film.WithId(id);
    }

    public void Update(Film film)
    {
        db.Execute(
            "UPDATE Films SET Title = @Title, Director = @Director, Genre = @Genre, " +
            "ReleaseYear = @ReleaseYear, RuntimeMinutes = @RuntimeMinutes, " +
            "CertifiedFresh = @CertifiedFresh WHERE Id = @Id",
            new
            {
                film.Id, film.Title, film.Director, film.Genre,
                film.ReleaseYear, film.RuntimeMinutes, film.CertifiedFresh
            });
    }

    public void Delete(int id) =>
        db.Execute("DELETE FROM Films WHERE Id = @id", new { id });
}
