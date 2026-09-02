using InboxPattern.Handlers;
using InboxPattern.Inbox;
using InboxPattern.Messages;

namespace InboxPattern.Receiver;

public sealed class WebhookReceiver(
    IInboxStore inboxStore,
    IMessageHandler<PaymentConfirmedMessage> paymentHandler,
    IMessageHandler<BookingCancelledMessage> cancellationHandler)
{
    public bool Receive(PaymentConfirmedMessage message)
    {
        if (!inboxStore.TryRecord(message.MessageId, nameof(PaymentConfirmedMessage)))
        {
            Console.WriteLine($"  [Inbox] Duplicate detected — {message.MessageId} skipped");
            return false;
        }

        paymentHandler.Handle(message);
        inboxStore.MarkProcessed(message.MessageId);
        return true;
    }

    public bool Receive(BookingCancelledMessage message)
    {
        if (!inboxStore.TryRecord(message.MessageId, nameof(BookingCancelledMessage)))
        {
            Console.WriteLine($"  [Inbox] Duplicate detected — {message.MessageId} skipped");
            return false;
        }

        cancellationHandler.Handle(message);
        inboxStore.MarkProcessed(message.MessageId);
        return true;
    }
}
