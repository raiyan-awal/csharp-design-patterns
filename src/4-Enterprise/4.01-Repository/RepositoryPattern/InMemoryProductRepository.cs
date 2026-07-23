using System.Linq.Expressions;

namespace RepositoryPattern;

// In-memory implementation of IRepository<Product>.
// In production this would be replaced by a SQL (EF Core / Dapper) or NoSQL
// implementation — the calling code would not need to change.
public sealed class InMemoryProductRepository : IRepository<Product>
{
    private readonly List<Product> _products = [];
    private int _nextId = 1;
    private readonly Lock _lock = new();

    public InMemoryProductRepository(bool seedData = false)
    {
        if (seedData) Seed();
    }

    public Task<Product?> GetByIdAsync(int id)
        => Task.FromResult(_products.FirstOrDefault(p => p.Id == id));

    public Task<IEnumerable<Product>> GetAllAsync()
        => Task.FromResult<IEnumerable<Product>>([.._products]);

    public Task<IEnumerable<Product>> FindAsync(Expression<Func<Product, bool>> predicate)
        => Task.FromResult<IEnumerable<Product>>([.._products.Where(predicate.Compile())]);

    public Task AddAsync(Product entity)
    {
        lock (_lock) { entity.Id = _nextId++; }
        _products.Add(entity);
        Console.WriteLine($"  [Repo] Added    → {entity}");
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Product entity)
    {
        var existing = _products.FirstOrDefault(p => p.Id == entity.Id)
            ?? throw new InvalidOperationException($"Product #{entity.Id} not found");

        existing.Name          = entity.Name;
        existing.Category      = entity.Category;
        existing.Price         = entity.Price;
        existing.StockQuantity = entity.StockQuantity;
        existing.IsActive      = entity.IsActive;

        Console.WriteLine($"  [Repo] Updated  → {existing}");
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id)
            ?? throw new InvalidOperationException($"Product #{id} not found");

        _products.Remove(product);
        Console.WriteLine($"  [Repo] Deleted  → {product}");
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(int id)
        => Task.FromResult(_products.Any(p => p.Id == id));

    public Task<int> CountAsync()
        => Task.FromResult(_products.Count);

    private void Seed()
    {
        _products.AddRange(
        [
            new() { Id = _nextId++, Name = "MacBook Air 15\"",          Category = "Electronics", Price = 1_699.99m, StockQuantity =  30, IsActive = true  },
            new() { Id = _nextId++, Name = "Sony WH-1000XM5 Headphones",Category = "Electronics", Price =   399.99m, StockQuantity =  45, IsActive = true  },
            new() { Id = _nextId++, Name = "Canon EOS R50 Camera",       Category = "Electronics", Price =   829.99m, StockQuantity =  12, IsActive = true  },
            new() { Id = _nextId++, Name = "The North Face Jacket",       Category = "Clothing",    Price =   299.99m, StockQuantity =  80, IsActive = true  },
            new() { Id = _nextId++, Name = "Levi's 501 Jeans",           Category = "Clothing",    Price =    98.99m, StockQuantity = 120, IsActive = true  },
            new() { Id = _nextId++, Name = "Instant Pot Duo 7-in-1",     Category = "Kitchen",     Price =   119.99m, StockQuantity =  60, IsActive = true  },
            new() { Id = _nextId++, Name = "Fitbit Charge 6",            Category = "Fitness",     Price =   179.99m, StockQuantity =   0, IsActive = false },
        ]);
    }
}
