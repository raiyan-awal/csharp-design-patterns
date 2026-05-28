namespace RepositoryPattern;

/// <summary>
/// REPOSITORY PATTERN DEMONSTRATION
///
/// This program demonstrates the Repository pattern by showing:
/// 1. How repositories abstract away data access details
/// 2. How to perform CRUD operations through a repository
/// 3. How to query data using the repository interface
/// 4. The benefits of working against an abstraction
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== REPOSITORY PATTERN DEMO ===\n");

        // Create a repository instance with seeded data
        // Notice: We declare it as IRepository<Product>, not InMemoryProductRepository
        // This is important - code should depend on the interface, not the implementation
        IRepository<Product> repository = new InMemoryProductRepository(seedData: true);

        Console.WriteLine();

        // DEMONSTRATION 1: Retrieving Data
        await DemoRetrieving(repository);

        // DEMONSTRATION 2: Adding New Entities
        await DemoAdding(repository);

        // DEMONSTRATION 3: Updating Entities
        await DemoUpdating(repository);

        // DEMONSTRATION 4: Deleting Entities
        await DemoDeleting(repository);

        // DEMONSTRATION 5: Querying with Predicates
        await DemoQuerying(repository);

        // DEMONSTRATION 6: The Power of Abstraction
        DemoAbstraction();

        // SUMMARY
        Console.WriteLine("\n=== KEY TAKEAWAYS ===");
        Console.WriteLine("✓ Repository acts like an in-memory collection of domain objects");
        Console.WriteLine("✓ Hides data access details from business logic");
        Console.WriteLine("✓ Easy to swap implementations (in-memory, SQL, NoSQL, API, etc.)");
        Console.WriteLine("✓ Greatly improves testability (can mock/fake for unit tests)");
        Console.WriteLine("✓ Centralizes data access logic");
        Console.WriteLine("✓ Follows Dependency Inversion Principle (depend on abstraction)");

        Console.WriteLine("\n=== WHEN TO USE REPOSITORY PATTERN ===");
        Console.WriteLine("✓ Domain-Driven Design (DDD) projects");
        Console.WriteLine("✓ Complex business logic that needs to be tested in isolation");
        Console.WriteLine("✓ Applications that might switch data sources");
        Console.WriteLine("✓ When you want to abstract away ORM-specific details");

        Console.WriteLine("\n=== WHEN NOT TO USE ===");
        Console.WriteLine("✗ Simple CRUD applications (direct ORM usage is fine)");
        Console.WriteLine("✗ When Entity Framework's DbSet already provides what you need");
        Console.WriteLine("✗ When the abstraction doesn't provide value");
        Console.WriteLine("✗ When you have very simple data access needs");
    }

    /// <summary>
    /// Demonstrate retrieving data from the repository.
    /// </summary>
    static async Task DemoRetrieving(IRepository<Product> repository)
    {
        Console.WriteLine("--- Demonstration 1: Retrieving Data ---");

        // Get a single product by ID
        var product = await repository.GetByIdAsync(1);
        if (product != null)
        {
            Console.WriteLine($"Found product: {product}");
        }

        // Get all products
        var allProducts = await repository.GetAllAsync();
        Console.WriteLine($"\nTotal products in repository: {await repository.CountAsync()}");

        Console.WriteLine("\nAll products:");
        foreach (var p in allProducts)
        {
            Console.WriteLine($"  {p}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrate adding new entities to the repository.
    /// </summary>
    static async Task DemoAdding(IRepository<Product> repository)
    {
        Console.WriteLine("--- Demonstration 2: Adding New Products ---\n");

        var newProduct = new Product
        {
            Name = "Gaming Monitor",
            Category = "Electronics",
            Price = 399.99m,
            StockQuantity = 25,
            IsActive = true
        };

        await repository.AddAsync(newProduct);

        // Verify it was added
        var retrieved = await repository.GetByIdAsync(newProduct.Id);
        Console.WriteLine($"Verified: {retrieved}");

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrate updating existing entities.
    /// </summary>
    static async Task DemoUpdating(IRepository<Product> repository)
    {
        Console.WriteLine("--- Demonstration 3: Updating Products ---\n");

        // Get an existing product
        var product = await repository.GetByIdAsync(1);

        if (product != null)
        {
            Console.WriteLine($"Before update: {product}");

            // Modify it
            product.Price = 899.99m; // Price drop!
            product.StockQuantity = 45;

            // Update in repository
            await repository.UpdateAsync(product);

            // Verify the update
            var updated = await repository.GetByIdAsync(1);
            Console.WriteLine($"After update:  {updated}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrate deleting entities.
    /// </summary>
    static async Task DemoDeleting(IRepository<Product> repository)
    {
        Console.WriteLine("--- Demonstration 4: Deleting Products ---\n");

        var countBefore = await repository.CountAsync();
        Console.WriteLine($"Products before deletion: {countBefore}");

        // Delete a product
        await repository.DeleteAsync(7); // Headphones (out of stock, inactive)

        var countAfter = await repository.CountAsync();
        Console.WriteLine($"Products after deletion: {countAfter}");

        // Verify it's gone
        var exists = await repository.ExistsAsync(7);
        Console.WriteLine($"Product #7 still exists: {exists}");

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrate querying with predicates.
    /// </summary>
    static async Task DemoQuerying(IRepository<Product> repository)
    {
        Console.WriteLine("--- Demonstration 5: Querying with Predicates ---\n");

        // Find all electronics
        var electronics = await repository.FindAsync(p => p.Category == "Electronics");
        Console.WriteLine("Electronics:");
        foreach (var product in electronics)
        {
            Console.WriteLine($"  {product}");
        }

        // Find products over $100
        var expensive = await repository.FindAsync(p => p.Price > 100);
        Console.WriteLine($"\nExpensive products (> $100): {expensive.Count()}");
        foreach (var product in expensive)
        {
            Console.WriteLine($"  {product}");
        }

        // Find products with low stock
        var lowStock = await repository.FindAsync(p => p.StockQuantity < 100 && p.IsActive);
        Console.WriteLine($"\nLow stock products (< 100 units): {lowStock.Count()}");
        foreach (var product in lowStock)
        {
            Console.WriteLine($"  {product}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrate the power of abstraction.
    /// </summary>
    static void DemoAbstraction()
    {
        Console.WriteLine("--- Demonstration 6: The Power of Abstraction ---\n");

        Console.WriteLine("Because we program against IRepository<Product>, we can easily swap implementations:");
        Console.WriteLine();
        Console.WriteLine("  IRepository<Product> repo = new InMemoryProductRepository();    // For testing");
        Console.WriteLine("  IRepository<Product> repo = new SqlProductRepository();         // For production");
        Console.WriteLine("  IRepository<Product> repo = new MongoProductRepository();       // Different DB");
        Console.WriteLine("  IRepository<Product> repo = new CachedProductRepository();      // With caching");
        Console.WriteLine("  IRepository<Product> repo = new ApiProductRepository();         // External API");
        Console.WriteLine();
        Console.WriteLine("The business logic using the repository DOESN'T CHANGE!");
        Console.WriteLine("This is Dependency Inversion Principle in action.");

        Console.WriteLine();
    }
}
