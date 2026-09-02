namespace InboxPattern.Messages;

public sealed record BookingCancelledMessage(
    string MessageId,
    string BookingId,
    string Reason,
    DateTimeOffset CancelledAt
);
