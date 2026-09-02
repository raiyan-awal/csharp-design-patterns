using ReadModelPattern.Events;

namespace ReadModelPattern.Infrastructure;

public sealed class InMemoryEventStore : IEventStore
{
    private readonly List<IDomainEvent> _events = [];

    public void Append(IDomainEvent @event) => _events.Add(@event);
    public IReadOnlyList<IDomainEvent> GetAll() => [.._events];
    public int Count => _events.Count;
}
