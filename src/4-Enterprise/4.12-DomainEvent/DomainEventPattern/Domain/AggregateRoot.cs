using DomainEventPattern.Events;

namespace DomainEventPattern.Domain;

// Base class for domain objects that produce domain events.
// Events are collected here and dispatched by the application layer after the
// operation and any persistence succeed — so handlers never run on a failed transaction.
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearEvents() => _domainEvents.Clear();
}
