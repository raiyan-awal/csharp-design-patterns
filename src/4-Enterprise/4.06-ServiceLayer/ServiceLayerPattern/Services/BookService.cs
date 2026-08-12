using ServiceLayerPattern.Domain;
using ServiceLayerPattern.Repositories;

namespace ServiceLayerPattern.Services;

public sealed class BookService(IBookRepository books) : IBookService
{
    private int _nextId = 1;

    public Book AddBook(string title, string author, string isbn, string genre, int copies)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.");
        if (string.IsNullOrWhiteSpace(author)) throw new ArgumentException("Author is required.");
        if (copies <= 0) throw new ArgumentException("At least one copy is required.");

        var book = new Book(_nextId++, title, author, isbn, genre, copies);
        books.Add(book);
        return book;
    }

    public Book GetBook(int id) =>
        books.GetById(id) ?? throw new KeyNotFoundException($"Book {id} not found.");

    public IReadOnlyList<Book> GetAllBooks() => books.GetAll();

    public IReadOnlyList<Book> SearchBooks(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return books.GetAll();
        return books.Search(query);
    }
}
