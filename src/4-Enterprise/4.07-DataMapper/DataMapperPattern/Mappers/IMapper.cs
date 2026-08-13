namespace DataMapperPattern.Mappers;

public interface IMapper<T>
{
    T? FindById(int id);
    IReadOnlyList<T> FindAll();
    T Insert(T entity);
    void Update(T entity);
    void Delete(int id);
}
