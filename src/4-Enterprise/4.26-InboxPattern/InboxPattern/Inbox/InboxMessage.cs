namespace InboxPattern.Inbox;

public enum InboxStatus { Pending, Processed }

public sealed record InboxMessage(
    string MessageId,
    string MessageType,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? ProcessedAt,
    InboxStatus Status
);
