using DomainEventPattern.Domain;
using DomainEventPattern.Events;
using DomainEventPattern.Handlers;

namespace DomainEventPattern.Infrastructure;

public sealed class DomainEventDispatcher
{
    // Each event type maps to a list of type-erased delegates, avoiding reflection at dispatch time.
    private readonly Dictionary<Type, List<Action<IDomainEvent>>> _handlers = new();

    public void Register<TEvent>(IDomainEventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        var key = typeof(TEvent);
        if (!_handlers.ContainsKey(key))
            _handlers[key] = new();
        // Wrap the strongly-typed Handle in an Action<IDomainEvent> so dispatch needs no reflection.
        _handlers[key].Add(e => handler.Handle((TEvent)e));
    }

    public void Dispatch(IEnumerable<IDomainEvent> events)
    {
        foreach (var domainEvent in events)
            if (_handlers.TryGetValue(domainEvent.GetType(), out var actions))
                foreach (var action in actions)
                    action(domainEvent);
    }

    // Convenience: dispatch all pending events from an aggregate and clear them.
    public void DispatchAndClear(AggregateRoot aggregate)
    {
        Dispatch(aggregate.DomainEvents);
        aggregate.ClearEvents();
    }
}
