namespace DomainEventPattern.Events;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
