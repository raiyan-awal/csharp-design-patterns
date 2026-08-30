namespace OutboxPattern.Core;

public sealed class OutboxRelay(
    IOutboxStore          outboxStore,
    Action<OutboxMessage> publish,
    Action<string>?       onPublished = null)
{
    public int ProcessPending()
    {
        var processed = 0;

        foreach (var message in outboxStore.GetUnprocessed())
        {
            try
            {
                publish(message);
                outboxStore.MarkProcessed(message.Id);
                onPublished?.Invoke(message.EventType);
                processed++;
            }
            catch { /* leave unprocessed; relay will retry on next run */ }
        }

        return processed;
    }
}
