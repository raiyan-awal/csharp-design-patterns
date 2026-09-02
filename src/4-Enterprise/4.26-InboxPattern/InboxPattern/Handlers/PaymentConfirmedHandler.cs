using InboxPattern.Messages;
using InboxPattern.Services;

namespace InboxPattern.Handlers;

public sealed class PaymentConfirmedHandler(BookingService bookingService)
    : IMessageHandler<PaymentConfirmedMessage>
{
    public void Handle(PaymentConfirmedMessage message) =>
        bookingService.ConfirmPayment(message.BookingId, message.AmountCAD);
}
