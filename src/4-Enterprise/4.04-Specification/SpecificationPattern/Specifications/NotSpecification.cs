using System.Linq.Expressions;

namespace SpecificationPattern;

public sealed class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _inner;

    public NotSpecification(Specification<T> inner) => _inner = inner;

    public override Expression<Func<T, bool>> ToExpression()
    {
        var param = Expression.Parameter(typeof(T), "x");
        var body  = Expression.Not(Expression.Invoke(_inner.ToExpression(), param));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}
