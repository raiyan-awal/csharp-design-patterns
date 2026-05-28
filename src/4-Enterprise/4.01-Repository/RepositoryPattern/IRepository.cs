namespace RepositoryPattern;

/// <summary>
/// REPOSITORY PATTERN - Generic Repository Interface
///
/// The Repository pattern mediates between the domain and data mapping layers,
/// acting like an in-memory collection of domain objects.
///
/// INTENT:
/// - Separate the logic that retrieves data from the business logic that acts on the data
/// - Provide a collection-like interface for accessing domain objects
/// - Decouple domain code from persistence infrastructure
///
/// BENEFITS:
/// ✓ Persistence ignorance - domain code doesn't know about databases
/// ✓ Testability - easy to mock/fake for unit tests
/// ✓ Centralized data access logic
/// ✓ Flexibility - can swap implementations (SQL, NoSQL, in-memory, etc.)
/// ✓ Query abstraction - hide complex queries behind simple methods
///
/// DRAWBACKS:
/// ✗ Can become a "leaky abstraction" if not carefully designed
/// ✗ May create unnecessary complexity for simple CRUD apps
/// ✗ Can lead to many small query methods (one per use case)
///
/// WHEN TO USE:
/// - Complex domain logic that should be decoupled from persistence
/// - Applications that might switch data sources
/// - When you need strong unit testing of business logic
/// - Domain-Driven Design (DDD) projects
///
/// WHEN NOT TO USE:
/// - Simple CRUD applications (direct ORM usage is fine)
/// - When you're using Entity Framework and DbSet already acts as a repository
/// - When the abstraction doesn't provide value
/// </summary>
/// <typeparam name="T">The entity type this repository manages</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Get an entity by its unique identifier.
    /// Returns null if not found.
    /// </summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// Get all entities of type T.
    /// Use with caution - can return large result sets!
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Find entities that match a predicate.
    /// This provides flexible querying without exposing the underlying data source.
    /// </summary>
    Task<IEnumerable<T>> FindAsync(Func<T, bool> predicate);

    /// <summary>
    /// Add a new entity to the repository.
    /// Note: This doesn't necessarily persist immediately - depends on implementation.
    /// </summary>
    Task AddAsync(T entity);

    /// <summary>
    /// Update an existing entity.
    /// </summary>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Delete an entity by its unique identifier.
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Check if an entity with the given ID exists.
    /// </summary>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Count the total number of entities.
    /// </summary>
    Task<int> CountAsync();
}
