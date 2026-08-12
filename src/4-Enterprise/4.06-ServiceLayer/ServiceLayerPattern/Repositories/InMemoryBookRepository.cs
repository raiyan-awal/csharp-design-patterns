using ServiceLayerPattern.Domain;

namespace ServiceLayerPattern.Repositories;

public sealed class InMemoryBookRepository : IBookRepository
{
    private readonly List<Book> _books = [];

    public Book? GetById(int id) => _books.FirstOrDefault(b => b.Id == id);

    public IReadOnlyList<Book> GetAll() => _books.AsReadOnly();

    public IReadOnlyList<Book> Search(string query)
    {
        var q = query.Trim().ToLowerInvariant();
        return _books
            .Where(b => b.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || b.Author.Contains(q, StringComparison.OrdinalIgnoreCase)
                     || b.Genre.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    public void Add(Book book) => _books.Add(book);

    public void Update(Book book)
    {
        var index = _books.FindIndex(b => b.Id == book.Id);
        if (index >= 0) _books[index] = book;
    }
}
