namespace OutboxPattern.Core;

public sealed class InMemoryOutboxStore : IOutboxStore
{
    private readonly List<OutboxMessage> _messages = [];
    private readonly Lock               _lock      = new();

    public IReadOnlyList<OutboxMessage> All
    {
        get { lock (_lock) return _messages.ToList().AsReadOnly(); }
    }

    public void Add(OutboxMessage message)
    {
        lock (_lock) _messages.Add(message);
    }

    public IReadOnlyList<OutboxMessage> GetUnprocessed()
    {
        lock (_lock) return _messages.Where(m => !m.IsProcessed).ToList();
    }

    public void MarkProcessed(Guid messageId)
    {
        lock (_lock)
        {
            var msg = _messages.FirstOrDefault(m => m.Id == messageId);
            if (msg is not null)
                msg.ProcessedAtUtc = DateTime.UtcNow;
        }
    }
}
