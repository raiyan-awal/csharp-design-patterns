namespace OutboxPattern.Core;

public interface IOutboxStore
{
    void Add(OutboxMessage message);
    IReadOnlyList<OutboxMessage> GetUnprocessed();
    void MarkProcessed(Guid messageId);
}
