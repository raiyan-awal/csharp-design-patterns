namespace ServiceLayerPattern.Domain;

public sealed class Book
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Author { get; init; } = "";
    public string Isbn { get; init; } = "";
    public string Genre { get; init; } = "";
    public int TotalCopies { get; init; }
    public int AvailableCopies { get; private set; }

    public bool IsAvailable => AvailableCopies > 0;

    public Book(int id, string title, string author, string isbn, string genre, int totalCopies)
    {
        Id = id;
        Title = title;
        Author = author;
        Isbn = isbn;
        Genre = genre;
        TotalCopies = totalCopies;
        AvailableCopies = totalCopies;
    }

    public void CheckOut()
    {
        if (AvailableCopies == 0)
            throw new InvalidOperationException($"No copies of '{Title}' are available.");
        AvailableCopies--;
    }

    public void Return()
    {
        if (AvailableCopies >= TotalCopies)
            throw new InvalidOperationException($"All copies of '{Title}' are already in.");
        AvailableCopies++;
    }
}
