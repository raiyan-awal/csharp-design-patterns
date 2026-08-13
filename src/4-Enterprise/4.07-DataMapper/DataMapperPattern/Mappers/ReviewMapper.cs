using System.Data;
using Dapper;
using DataMapperPattern.Domain;

namespace DataMapperPattern.Mappers;

public sealed class ReviewMapper(IDbConnection db) : IMapper<Review>
{
    private sealed class ReviewRow
    {
        public int Id { get; init; }
        public int FilmId { get; init; }
        public string ReviewerName { get; init; } = "";
        public int Score { get; init; }
        public string Comment { get; init; } = "";
        public string ReviewedAt { get; init; } = "";

        public Review ToDomain() =>
            new(Id, FilmId, ReviewerName, Score, Comment, DateTime.Parse(ReviewedAt));
    }

    public Review? FindById(int id)
    {
        var row = db.QuerySingleOrDefault<ReviewRow>(
            "SELECT Id, FilmId, ReviewerName, Score, Comment, ReviewedAt " +
            "FROM Reviews WHERE Id = @id", new { id });
        return row?.ToDomain();
    }

    public IReadOnlyList<Review> FindAll()
    {
        var rows = db.Query<ReviewRow>(
            "SELECT Id, FilmId, ReviewerName, Score, Comment, ReviewedAt " +
            "FROM Reviews ORDER BY ReviewedAt DESC");
        return rows.Select(r => r.ToDomain()).ToList().AsReadOnly();
    }

    public IReadOnlyList<Review> FindByFilmId(int filmId)
    {
        var rows = db.Query<ReviewRow>(
            "SELECT Id, FilmId, ReviewerName, Score, Comment, ReviewedAt " +
            "FROM Reviews WHERE FilmId = @filmId ORDER BY ReviewedAt DESC", new { filmId });
        return rows.Select(r => r.ToDomain()).ToList().AsReadOnly();
    }

    public double AverageScore(int filmId)
    {
        return db.ExecuteScalar<double>(
            "SELECT COALESCE(AVG(CAST(Score AS REAL)), 0) FROM Reviews WHERE FilmId = @filmId",
            new { filmId });
    }

    public Review Insert(Review review)
    {
        var id = db.ExecuteScalar<int>(
            "INSERT INTO Reviews (FilmId, ReviewerName, Score, Comment, ReviewedAt) " +
            "VALUES (@FilmId, @ReviewerName, @Score, @Comment, @ReviewedAt); " +
            "SELECT last_insert_rowid();",
            new
            {
                review.FilmId, review.ReviewerName, review.Score,
                review.Comment, ReviewedAt = review.ReviewedAt.ToString("O")
            });
        return review.WithId(id);
    }

    public void Update(Review review)
    {
        db.Execute(
            "UPDATE Reviews SET FilmId = @FilmId, ReviewerName = @ReviewerName, " +
            "Score = @Score, Comment = @Comment, ReviewedAt = @ReviewedAt WHERE Id = @Id",
            new
            {
                review.Id, review.FilmId, review.ReviewerName,
                review.Score, review.Comment, ReviewedAt = review.ReviewedAt.ToString("O")
            });
    }

    public void Delete(int id) =>
        db.Execute("DELETE FROM Reviews WHERE Id = @id", new { id });
}
