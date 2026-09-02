namespace InboxPattern.Inbox;

public sealed class InMemoryInboxStore : IInboxStore
{
    private readonly Dictionary<string, InboxMessage> _messages = [];
    private readonly Lock _lock = new();

    public bool TryRecord(string messageId, string messageType)
    {
        lock (_lock)
        {
            if (_messages.ContainsKey(messageId))
                return false;

            _messages[messageId] = new InboxMessage(
                messageId, messageType, DateTimeOffset.UtcNow, null, InboxStatus.Pending);
            return true;
        }
    }

    public void MarkProcessed(string messageId)
    {
        lock (_lock)
        {
            if (_messages.TryGetValue(messageId, out var msg))
                _messages[messageId] = msg with
                {
                    ProcessedAt = DateTimeOffset.UtcNow,
                    Status = InboxStatus.Processed
                };
        }
    }

    public IReadOnlyList<InboxMessage> GetAll()
    {
        lock (_lock) return [.._messages.Values];
    }
}
