using CacheAsidePattern.Domain;

namespace CacheAsidePattern.Data;

public sealed class InMemoryBookRepository : IBookRepository
{
    private readonly Dictionary<string, Book> _store = [];

    public int LoadCount { get; private set; }

    public void Seed(IEnumerable<Book> books)
    {
        foreach (var book in books)
            _store[book.Id] = book;
    }

    public Book? FindById(string id)
    {
        LoadCount++;
        return _store.TryGetValue(id, out var book) ? book : null;
    }

    public IReadOnlyList<Book> FindByAuthor(string author)
    {
        LoadCount++;
        return _store.Values.Where(b => b.Author == author).ToList();
    }

    public void Save(Book book) => _store[book.Id] = book;

    public void Delete(string id) => _store.Remove(id);
}
