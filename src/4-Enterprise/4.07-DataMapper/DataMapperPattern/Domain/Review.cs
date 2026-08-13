namespace DataMapperPattern.Domain;

public sealed class Review
{
    public int Id { get; init; }
    public int FilmId { get; init; }
    public string ReviewerName { get; init; }
    public int Score { get; init; }
    public string Comment { get; init; }
    public DateTime ReviewedAt { get; init; }

    public Review(int id, int filmId, string reviewerName, int score, string comment, DateTime reviewedAt)
    {
        if (score < 1 || score > 10)
            throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 1 and 10.");

        Id = id;
        FilmId = filmId;
        ReviewerName = reviewerName;
        Score = score;
        Comment = comment;
        ReviewedAt = reviewedAt;
    }

    public Review WithId(int id) =>
        new(id, FilmId, ReviewerName, Score, Comment, ReviewedAt);
}
