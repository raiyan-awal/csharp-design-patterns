namespace EventSourcingPattern.Infrastructure;

using EventSourcingPattern.Domain.Events;

public sealed class InMemoryEventStore : IEventStore
{
    private readonly Dictionary<int, List<IDomainEvent>> _streams = new();

    public void Append(int streamId, IEnumerable<IDomainEvent> events)
    {
        if (!_streams.TryGetValue(streamId, out var stream))
        {
            stream = new List<IDomainEvent>();
            _streams[streamId] = stream;
        }
        stream.AddRange(events);
    }

    public IReadOnlyList<IDomainEvent> Load(int streamId) =>
        _streams.TryGetValue(streamId, out var stream) ? stream : [];

    public IReadOnlyList<IDomainEvent> LoadFrom(int streamId, int fromVersion) =>
        _streams.TryGetValue(streamId, out var stream)
            ? stream.Skip(fromVersion).ToList()
            : [];
}
