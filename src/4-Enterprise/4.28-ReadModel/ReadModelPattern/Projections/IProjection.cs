using ReadModelPattern.Events;

namespace ReadModelPattern.Projections;

public interface IProjection
{
    void Apply(IDomainEvent @event);
    void Reset();
}
