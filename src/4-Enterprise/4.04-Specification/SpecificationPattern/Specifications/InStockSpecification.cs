using System.Linq.Expressions;

namespace SpecificationPattern;

public sealed class InStockSpecification : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => p => p.StockQuantity > 0;
}
