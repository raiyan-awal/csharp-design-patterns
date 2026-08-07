using System.Linq.Expressions;

namespace SpecificationPattern;

// Abstract base providing And / Or / Not combinators for free.
// Concrete specs only need to implement ToExpression().
// IsSatisfiedBy compiles the expression once (lazy, cached) so repeated
// in-memory checks do not re-compile on every call.
public abstract class Specification<T> : ISpecification<T>
{
    private Func<T, bool>? _compiled;

    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity)
    {
        _compiled ??= ToExpression().Compile();
        return _compiled(entity);
    }

    public Specification<T> And(Specification<T> other)  => new AndSpecification<T>(this, other);
    public Specification<T> Or(Specification<T> other)   => new OrSpecification<T>(this, other);
    public Specification<T> Not()                        => new NotSpecification<T>(this);
}
