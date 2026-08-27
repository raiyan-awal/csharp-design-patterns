namespace EventSourcingPattern.Infrastructure;

using EventSourcingPattern.Domain.Events;

public interface IEventStore
{
    void                        Append(int streamId, IEnumerable<IDomainEvent> events);
    IReadOnlyList<IDomainEvent> Load(int streamId);
    IReadOnlyList<IDomainEvent> LoadFrom(int streamId, int fromVersion);
}
