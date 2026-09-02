namespace InboxPattern.Inbox;

public interface IInboxStore
{
    // Returns true if the message is new and was recorded; false if it is a duplicate.
    bool TryRecord(string messageId, string messageType);
    void MarkProcessed(string messageId);
    IReadOnlyList<InboxMessage> GetAll();
}
