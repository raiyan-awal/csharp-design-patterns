namespace InboxPattern.Domain;

public sealed record Booking(
    string Id,
    string EventId,
    string CustomerEmail,
    decimal AmountCAD,
    BookingStatus Status
);
