using System.Linq.Expressions;

namespace SpecificationPattern;

// A Specification encapsulates a business rule as a reusable, combinable object.
// IsSatisfiedBy runs in-memory (LINQ-to-Objects).
// ToExpression() returns an expression tree that an ORM (EF Core) can translate to SQL.
public interface ISpecification<T>
{
    bool                       IsSatisfiedBy(T entity);
    Expression<Func<T, bool>>  ToExpression();
}
