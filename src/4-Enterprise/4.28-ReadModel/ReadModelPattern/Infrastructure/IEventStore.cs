using ReadModelPattern.Events;

namespace ReadModelPattern.Infrastructure;

public interface IEventStore
{
    void Append(IDomainEvent @event);
    IReadOnlyList<IDomainEvent> GetAll();
    int Count { get; }
}
