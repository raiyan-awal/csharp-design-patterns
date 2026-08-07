using System.Linq.Expressions;

namespace SpecificationPattern;

public sealed class MinRatingSpecification : Specification<Product>
{
    private readonly double _minRating;

    public MinRatingSpecification(double minRating) => _minRating = minRating;

    public override Expression<Func<Product, bool>> ToExpression()
        => p => p.Rating >= _minRating;
}
