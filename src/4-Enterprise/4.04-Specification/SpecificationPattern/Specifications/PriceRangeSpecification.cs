using System.Linq.Expressions;

namespace SpecificationPattern;

public sealed class PriceRangeSpecification : Specification<Product>
{
    private readonly decimal _min;
    private readonly decimal _max;

    public PriceRangeSpecification(decimal min, decimal max)
    {
        _min = min;
        _max = max;
    }

    public override Expression<Func<Product, bool>> ToExpression()
        => p => p.Price >= _min && p.Price <= _max;
}
