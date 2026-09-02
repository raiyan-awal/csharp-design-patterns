using InboxPattern.Domain;

namespace InboxPattern.Services;

public sealed class BookingService
{
    private readonly Dictionary<string, Booking> _bookings = [];

    public int ConfirmCount { get; private set; }
    public int CancelCount { get; private set; }

    public void AddPending(string bookingId, string eventId, string customerEmail, decimal amountCAD)
    {
        _bookings[bookingId] = new Booking(bookingId, eventId, customerEmail, amountCAD, BookingStatus.Pending);
    }

    public void ConfirmPayment(string bookingId, decimal amountCAD)
    {
        if (!_bookings.TryGetValue(bookingId, out var booking))
            throw new KeyNotFoundException($"Booking {bookingId} not found.");

        _bookings[bookingId] = booking with { Status = BookingStatus.Confirmed };
        ConfirmCount++;
        Console.WriteLine($"  [BookingService] Booking {bookingId} confirmed — ${amountCAD:F2} CAD");
    }

    public void CancelBooking(string bookingId, string reason)
    {
        if (!_bookings.TryGetValue(bookingId, out var booking))
            throw new KeyNotFoundException($"Booking {bookingId} not found.");

        _bookings[bookingId] = booking with { Status = BookingStatus.Cancelled };
        CancelCount++;
        Console.WriteLine($"  [BookingService] Booking {bookingId} cancelled — {reason}");
    }

    public Booking? Find(string bookingId) =>
        _bookings.TryGetValue(bookingId, out var b) ? b : null;

    public IReadOnlyList<Booking> GetAll() => [.._bookings.Values];
}
