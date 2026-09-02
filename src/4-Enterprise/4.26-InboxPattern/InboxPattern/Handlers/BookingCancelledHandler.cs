using InboxPattern.Messages;
using InboxPattern.Services;

namespace InboxPattern.Handlers;

public sealed class BookingCancelledHandler(BookingService bookingService)
    : IMessageHandler<BookingCancelledMessage>
{
    public void Handle(BookingCancelledMessage message) =>
        bookingService.CancelBooking(message.BookingId, message.Reason);
}
