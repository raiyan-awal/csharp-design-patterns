using System.Linq.Expressions;

namespace SpecificationPattern;

public sealed class ActiveSpecification : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => p => p.IsActive;
}
