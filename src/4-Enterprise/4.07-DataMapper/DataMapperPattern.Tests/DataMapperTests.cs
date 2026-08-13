using Microsoft.Data.Sqlite;
using DataMapperPattern.Domain;
using DataMapperPattern.Mappers;

namespace DataMapperPattern.Tests;

public sealed class DataMapperTests : IDisposable
{
    private readonly SqliteConnection _db;
    private readonly FilmMapper _films;
    private readonly ReviewMapper _reviews;

    public DataMapperTests()
    {
        _db = new SqliteConnection("Data Source=:memory:");
        _db.Open();
        Schema.Create(_db);
        _films   = new FilmMapper(_db);
        _reviews = new ReviewMapper(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── helpers ───────────────────────────────────────────────────────────────

    private Film SeedFilm(string title = "Incendies", string director = "Denis Villeneuve",
                           string genre = "Thriller", int year = 2010, int runtime = 130) =>
        _films.Insert(new Film(0, title, director, genre, year, runtime));

    private Review SeedReview(int filmId, int score = 9) =>
        _reviews.Insert(new Review(0, filmId, "Alice Tremblay", score, "Great film.", DateTime.UtcNow));

    // ── FilmMapper — insert and find ──────────────────────────────────────────

    [Fact]
    public void Insert_AssignsId()
    {
        var film = SeedFilm();
        Assert.True(film.Id > 0);
    }

    [Fact]
    public void Insert_FindById_RoundTrip()
    {
        var inserted = SeedFilm("Atanarjuat: The Fast Runner", "Zacharias Kunuk", "Drama", 2001, 172);
        var found = _films.FindById(inserted.Id);

        Assert.NotNull(found);
        Assert.Equal(inserted.Id,            found.Id);
        Assert.Equal("Atanarjuat: The Fast Runner", found.Title);
        Assert.Equal("Zacharias Kunuk",      found.Director);
        Assert.Equal("Drama",                found.Genre);
        Assert.Equal(2001,                   found.ReleaseYear);
        Assert.Equal(172,                    found.RuntimeMinutes);
    }

    [Fact]
    public void FindById_ReturnsNull_WhenNotFound()
    {
        Assert.Null(_films.FindById(999));
    }

    [Fact]
    public void FindAll_ReturnsAllFilms()
    {
        SeedFilm("Film A");
        SeedFilm("Film B");
        SeedFilm("Film C");
        Assert.Equal(3, _films.FindAll().Count);
    }

    [Fact]
    public void FindAll_ReturnsEmpty_WhenNoFilms()
    {
        Assert.Empty(_films.FindAll());
    }

    [Fact]
    public void FindAll_OrderedByReleaseYear()
    {
        SeedFilm("Newer", year: 2015);
        SeedFilm("Older", year: 2000);
        SeedFilm("Middle", year: 2008);

        var all = _films.FindAll();
        Assert.Equal(2000, all[0].ReleaseYear);
        Assert.Equal(2008, all[1].ReleaseYear);
        Assert.Equal(2015, all[2].ReleaseYear);
    }

    // ── FilmMapper — update and delete ────────────────────────────────────────

    [Fact]
    public void Update_PersistsCertifiedFresh()
    {
        var film = SeedFilm();
        Assert.False(film.CertifiedFresh);

        film.Certify();
        _films.Update(film);

        var reloaded = _films.FindById(film.Id)!;
        Assert.True(reloaded.CertifiedFresh);
    }

    [Fact]
    public void Update_PersistsFieldChanges()
    {
        var film = SeedFilm("Original Title");
        var updated = new Film(film.Id, "Updated Title", film.Director, film.Genre,
                               film.ReleaseYear, film.RuntimeMinutes);
        _films.Update(updated);

        Assert.Equal("Updated Title", _films.FindById(film.Id)!.Title);
    }

    [Fact]
    public void Delete_RemovesFilm()
    {
        var film = SeedFilm();
        _films.Delete(film.Id);
        Assert.Null(_films.FindById(film.Id));
        Assert.Empty(_films.FindAll());
    }

    // ── FilmMapper — queries ──────────────────────────────────────────────────

    [Fact]
    public void FindByGenre_ReturnsMatchingFilms()
    {
        SeedFilm("Film A", genre: "Drama");
        SeedFilm("Film B", genre: "Drama");
        SeedFilm("Film C", genre: "Comedy");

        var dramas = _films.FindByGenre("Drama");
        Assert.Equal(2, dramas.Count);
        Assert.All(dramas, f => Assert.Equal("Drama", f.Genre));
    }

    [Fact]
    public void FindByDirector_ReturnsMatchingFilms()
    {
        SeedFilm("Incendies",    director: "Denis Villeneuve");
        SeedFilm("Arrival",      director: "Denis Villeneuve");
        SeedFilm("Sweet Hereafter", director: "Atom Egoyan");

        var villeneuve = _films.FindByDirector("Denis Villeneuve");
        Assert.Equal(2, villeneuve.Count);
        Assert.All(villeneuve, f => Assert.Equal("Denis Villeneuve", f.Director));
    }

    // ── ReviewMapper ──────────────────────────────────────────────────────────

    [Fact]
    public void ReviewInsert_AssignsId()
    {
        var film   = SeedFilm();
        var review = SeedReview(film.Id);
        Assert.True(review.Id > 0);
    }

    [Fact]
    public void ReviewInsert_FindById_RoundTrip()
    {
        var film   = SeedFilm();
        var inserted = _reviews.Insert(new Review(0, film.Id, "Marie-Claire", 8, "Great.", DateTime.UtcNow));
        var found    = _reviews.FindById(inserted.Id);

        Assert.NotNull(found);
        Assert.Equal(film.Id,       found.FilmId);
        Assert.Equal("Marie-Claire", found.ReviewerName);
        Assert.Equal(8,             found.Score);
    }

    [Fact]
    public void FindByFilmId_ReturnsOnlyThatFilmsReviews()
    {
        var film1 = SeedFilm("Film 1");
        var film2 = SeedFilm("Film 2");
        SeedReview(film1.Id, score: 9);
        SeedReview(film1.Id, score: 8);
        SeedReview(film2.Id, score: 7);

        var film1Reviews = _reviews.FindByFilmId(film1.Id);
        Assert.Equal(2, film1Reviews.Count);
        Assert.All(film1Reviews, r => Assert.Equal(film1.Id, r.FilmId));
    }

    [Fact]
    public void FindByFilmId_ReturnsEmpty_WhenNoReviews()
    {
        var film = SeedFilm();
        Assert.Empty(_reviews.FindByFilmId(film.Id));
    }

    [Fact]
    public void AverageScore_CalculatesCorrectly()
    {
        var film = SeedFilm();
        SeedReview(film.Id, score: 8);
        SeedReview(film.Id, score: 10);

        var avg = _reviews.AverageScore(film.Id);
        Assert.Equal(9.0, avg, precision: 1);
    }

    [Fact]
    public void AverageScore_ReturnsZero_WhenNoReviews()
    {
        var film = SeedFilm();
        Assert.Equal(0.0, _reviews.AverageScore(film.Id));
    }

    // ── Domain purity ─────────────────────────────────────────────────────────

    [Fact]
    public void Film_HasNoPublicDbProperties()
    {
        var filmType = typeof(Film);
        var props = filmType.GetProperties();

        // Film should have no connection, command, or mapper properties
        Assert.DoesNotContain(props, p =>
            p.PropertyType.Name.Contains("Connection") ||
            p.PropertyType.Name.Contains("Command") ||
            p.PropertyType.Name.Contains("Mapper"));
    }

    [Fact]
    public void Film_Certify_Decertify_WorksInMemory()
    {
        var film = new Film(0, "Test", "Director", "Drama", 2020, 100);
        Assert.False(film.CertifiedFresh);
        film.Certify();
        Assert.True(film.CertifiedFresh);
        film.Decertify();
        Assert.False(film.CertifiedFresh);
    }

    [Fact]
    public void Review_ThrowsOnInvalidScore()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Review(0, 1, "Reviewer", 0, "Comment", DateTime.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Review(0, 1, "Reviewer", 11, "Comment", DateTime.UtcNow));
    }
}
