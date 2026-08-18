namespace IdentityMapPattern.Domain;

public sealed class Artwork
{
    public int Id { get; init; }
    public string Title { get; init; }
    public int ArtistId { get; init; }
    public string Medium { get; init; }
    public int Year { get; init; }
    public decimal ValuationCad { get; init; }
    public bool OnDisplay { get; private set; }

    public Artwork(int id, string title, int artistId, string medium,
                   int year, decimal valuationCad, bool onDisplay = false)
    {
        Id = id;
        Title = title;
        ArtistId = artistId;
        Medium = medium;
        Year = year;
        ValuationCad = valuationCad;
        OnDisplay = onDisplay;
    }

    public void PutOnDisplay() => OnDisplay = true;
    public void PutInStorage() => OnDisplay = false;

    public Artwork WithId(int id) =>
        new(id, Title, ArtistId, Medium, Year, ValuationCad, OnDisplay);
}
