using ReadModelPattern.Events;
using ReadModelPattern.Infrastructure;
using ReadModelPattern.Projections;

namespace ReadModelPattern.Engine;

// Routes incoming events to all registered projections and owns the event store.
// Rebuild replays the entire event history so a new or corrected projection
// can catch up without losing any past events.
public sealed class ProjectionEngine(IEventStore eventStore)
{
    private readonly List<IProjection> _projections = [];

    public void Register(IProjection projection) => _projections.Add(projection);

    public void Append(IDomainEvent @event)
    {
        eventStore.Append(@event);
        foreach (var p in _projections)
            p.Apply(@event);
    }

    // Clears every registered projection's read model store, then replays
    // all stored events from the beginning. Used when a new projection is
    // added or an existing one is corrected — no events are ever lost.
    public void Rebuild()
    {
        foreach (var p in _projections)
            p.Reset();
        foreach (var @event in eventStore.GetAll())
            foreach (var p in _projections)
                p.Apply(@event);
    }

    public IReadOnlyList<IDomainEvent> GetStoredEvents() => eventStore.GetAll();
}
