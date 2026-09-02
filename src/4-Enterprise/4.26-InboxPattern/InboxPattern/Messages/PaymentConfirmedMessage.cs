namespace InboxPattern.Messages;

public sealed record PaymentConfirmedMessage(
    string MessageId,
    string BookingId,
    decimal AmountCAD,
    DateTimeOffset PaidAt
);
