namespace SpecificationPattern;

// In-memory repository whose Find method accepts a Specification<Product>
// rather than a raw Func or Expression — the caller never writes a predicate.
// In a real EF Core repository, Find would call:
//   _context.Products.Where(spec.ToExpression()).ToListAsync()
public sealed class ProductRepository
{
    private readonly List<Product> _products;

    public ProductRepository(IEnumerable<Product> products)
        => _products = [..products];

    public IEnumerable<Product> Find(Specification<Product> spec)
        => _products.Where(spec.IsSatisfiedBy);

    public bool Any(Specification<Product> spec)
        => _products.Any(spec.IsSatisfiedBy);

    public int Count(Specification<Product> spec)
        => _products.Count(spec.IsSatisfiedBy);
}
