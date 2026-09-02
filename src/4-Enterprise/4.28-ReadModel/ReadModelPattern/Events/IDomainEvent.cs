namespace ReadModelPattern.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
