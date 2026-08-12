using ServiceLayerPattern.Domain;

namespace ServiceLayerPattern.Repositories;

public interface IBookRepository
{
    Book? GetById(int id);
    IReadOnlyList<Book> GetAll();
    IReadOnlyList<Book> Search(string query);
    void Add(Book book);
    void Update(Book book);
}
