using System.Collections.Concurrent;

namespace PublishSubscribePattern.Core;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    private readonly Lock _lock = new();

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        lock (_lock)
        {
            var handlers = _handlers.GetOrAdd(typeof(TEvent), _ => []);
            handlers.Add(handler);
        }
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        lock (_lock)
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
                handlers.Remove(handler);
        }
    }

    public void Publish<TEvent>(TEvent @event) where TEvent : class
    {
        List<Delegate> snapshot;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
                return;
            snapshot = [..handlers];
        }

        foreach (var handler in snapshot)
            ((Action<TEvent>)handler)(@event);
    }
}
