using DomainEventPattern.Events;

namespace DomainEventPattern.Handlers;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    void Handle(TEvent domainEvent);
}
