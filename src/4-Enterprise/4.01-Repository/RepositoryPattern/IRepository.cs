using System.Linq.Expressions;

namespace RepositoryPattern;

// Generic repository interface — acts like an in-memory collection of domain objects.
// Business logic depends only on this interface; it never knows which storage backend
// is behind it (SQL, in-memory, API, cache).
public interface IRepository<T> where T : class
{
    Task<T?>              GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task                 AddAsync(T entity);
    Task                 UpdateAsync(T entity);
    Task                 DeleteAsync(int id);
    Task<bool>           ExistsAsync(int id);
    Task<int>            CountAsync();
}
