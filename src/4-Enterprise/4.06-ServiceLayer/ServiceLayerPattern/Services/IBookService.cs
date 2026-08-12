using ServiceLayerPattern.Domain;

namespace ServiceLayerPattern.Services;

public interface IBookService
{
    Book AddBook(string title, string author, string isbn, string genre, int copies);
    Book GetBook(int id);
    IReadOnlyList<Book> GetAllBooks();
    IReadOnlyList<Book> SearchBooks(string query);
}
