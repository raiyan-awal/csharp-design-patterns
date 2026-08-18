namespace IdentityMapPattern.Domain;

public sealed class Artist
{
    public int Id { get; init; }
    public string Name { get; private set; }
    public string Nationality { get; init; }
    public int BirthYear { get; init; }

    public Artist(int id, string name, string nationality, int birthYear)
    {
        Id = id;
        Name = name;
        Nationality = nationality;
        BirthYear = birthYear;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name cannot be blank.", nameof(newName));
        Name = newName;
    }

    public Artist WithId(int id) => new(id, Name, Nationality, BirthYear);
}
