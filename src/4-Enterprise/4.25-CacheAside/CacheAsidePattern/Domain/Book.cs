namespace CacheAsidePattern.Domain;

public sealed record Book(
    string Id,
    string Title,
    string Author,
    string Isbn,
    decimal PriceCAD,
    string Genre
);
