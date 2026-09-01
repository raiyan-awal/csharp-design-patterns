using CacheAsidePattern.Domain;

namespace CacheAsidePattern.Data;

public interface IBookRepository
{
    Book? FindById(string id);
    IReadOnlyList<Book> FindByAuthor(string author);
    void Save(Book book);
    void Delete(string id);
}
