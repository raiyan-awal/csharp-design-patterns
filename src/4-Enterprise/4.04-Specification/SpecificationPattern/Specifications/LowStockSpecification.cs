using System.Linq.Expressions;

namespace SpecificationPattern;

// Products that are active but running low — candidates for reorder alerts.
public sealed class LowStockSpecification : Specification<Product>
{
    private readonly int _threshold;

    public LowStockSpecification(int threshold = 10) => _threshold = threshold;

    public override Expression<Func<Product, bool>> ToExpression()
        => p => p.StockQuantity > 0 && p.StockQuantity <= _threshold;
}
