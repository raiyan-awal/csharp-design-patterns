using System.Linq.Expressions;

namespace SpecificationPattern;

public sealed class CategorySpecification : Specification<Product>
{
    private readonly string _category;

    public CategorySpecification(string category) => _category = category;

    public override Expression<Func<Product, bool>> ToExpression()
        => p => p.Category == _category;
}
