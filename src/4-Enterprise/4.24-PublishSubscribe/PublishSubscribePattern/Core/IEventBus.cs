namespace PublishSubscribePattern.Core;

public interface IEventBus
{
    void Publish<TEvent>(TEvent @event) where TEvent : class;
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
    void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
}
