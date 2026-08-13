namespace DataMapperPattern.Domain;

public sealed class Film
{
    public int Id { get; init; }
    public string Title { get; init; }
    public string Director { get; init; }
    public string Genre { get; init; }
    public int ReleaseYear { get; init; }
    public int RuntimeMinutes { get; init; }
    public bool CertifiedFresh { get; private set; }

    public Film(int id, string title, string director, string genre,
                int releaseYear, int runtimeMinutes, bool certifiedFresh = false)
    {
        Id = id;
        Title = title;
        Director = director;
        Genre = genre;
        ReleaseYear = releaseYear;
        RuntimeMinutes = runtimeMinutes;
        CertifiedFresh = certifiedFresh;
    }

    public void Certify()   => CertifiedFresh = true;
    public void Decertify() => CertifiedFresh = false;

    public Film WithId(int id) =>
        new(id, Title, Director, Genre, ReleaseYear, RuntimeMinutes, CertifiedFresh);
}
