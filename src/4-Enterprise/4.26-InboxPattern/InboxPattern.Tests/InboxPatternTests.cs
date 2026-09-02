using InboxPattern.Handlers;
using InboxPattern.Inbox;
using InboxPattern.Messages;
using InboxPattern.Receiver;
using InboxPattern.Services;

namespace InboxPattern.Tests;

// ── Helpers ───────────────────────────────────────────────────────────────────

file static class Factory
{
    public static PaymentConfirmedMessage Payment(
        string messageId = "msg-001",
        string bookingId = "bk-001",
        decimal amount   = 189.99m) =>
        new(messageId, bookingId, amount, DateTimeOffset.UtcNow);

    public static BookingCancelledMessage Cancellation(
        string messageId = "msg-cancel-001",
        string bookingId = "bk-001",
        string reason    = "Customer request") =>
        new(messageId, bookingId, reason, DateTimeOffset.UtcNow);

    public static (InMemoryInboxStore Inbox, BookingService Bookings, WebhookReceiver Receiver)
        MakeReceiver(params string[] bookingIds)
    {
        var inbox    = new InMemoryInboxStore();
        var bookings = new BookingService();
        foreach (var id in bookingIds)
            bookings.AddPending(id, "evt-1", $"{id}@test.ca", 99m);
        var receiver = new WebhookReceiver(
            inbox,
            new PaymentConfirmedHandler(bookings),
            new BookingCancelledHandler(bookings));
        return (inbox, bookings, receiver);
    }
}

// ── Suite 1: InMemoryInboxStore ───────────────────────────────────────────────

public sealed class InMemoryInboxStore_Tests
{
    [Fact]
    public void TryRecord_NewMessage_ReturnsTrue()
    {
        var store = new InMemoryInboxStore();
        Assert.True(store.TryRecord("msg-1", "PaymentConfirmed"));
    }

    [Fact]
    public void TryRecord_DuplicateMessageId_ReturnsFalse()
    {
        var store = new InMemoryInboxStore();
        store.TryRecord("msg-1", "PaymentConfirmed");
        Assert.False(store.TryRecord("msg-1", "PaymentConfirmed"));
    }

    [Fact]
    public void TryRecord_DifferentIds_BothReturnTrue()
    {
        var store = new InMemoryInboxStore();
        Assert.True(store.TryRecord("msg-1", "PaymentConfirmed"));
        Assert.True(store.TryRecord("msg-2", "PaymentConfirmed"));
    }

    [Fact]
    public void TryRecord_NewMessage_StartsAsPending()
    {
        var store = new InMemoryInboxStore();
        store.TryRecord("msg-1", "PaymentConfirmed");
        var msg = store.GetAll().Single();
        Assert.Equal(InboxStatus.Pending, msg.Status);
        Assert.Null(msg.ProcessedAt);
    }

    [Fact]
    public void MarkProcessed_SetsStatusAndTimestamp()
    {
        var store = new InMemoryInboxStore();
        store.TryRecord("msg-1", "PaymentConfirmed");
        store.MarkProcessed("msg-1");
        var msg = store.GetAll().Single();
        Assert.Equal(InboxStatus.Processed, msg.Status);
        Assert.NotNull(msg.ProcessedAt);
    }

    [Fact]
    public void GetAll_ReturnsAllRecordedMessages()
    {
        var store = new InMemoryInboxStore();
        store.TryRecord("msg-1", "PaymentConfirmed");
        store.TryRecord("msg-2", "BookingCancelled");
        Assert.Equal(2, store.GetAll().Count);
    }

    [Fact]
    public void DuplicateAttempt_DoesNotCreateSecondRecord()
    {
        var store = new InMemoryInboxStore();
        store.TryRecord("msg-1", "PaymentConfirmed");
        store.TryRecord("msg-1", "PaymentConfirmed");  // duplicate
        Assert.Single(store.GetAll());
    }
}

// ── Suite 2: WebhookReceiver — idempotency ────────────────────────────────────

public sealed class WebhookReceiver_Idempotency
{
    [Fact]
    public void FirstDelivery_ReturnsTrue()
    {
        var (_, _, receiver) = Factory.MakeReceiver("bk-001");
        Assert.True(receiver.Receive(Factory.Payment()));
    }

    [Fact]
    public void SecondDelivery_SameMessageId_ReturnsFalse()
    {
        var (_, _, receiver) = Factory.MakeReceiver("bk-001");
        var msg = Factory.Payment();
        receiver.Receive(msg);
        Assert.False(receiver.Receive(msg));
    }

    [Fact]
    public void MultipleRetries_AllReturnFalse()
    {
        var (_, _, receiver) = Factory.MakeReceiver("bk-001");
        var msg = Factory.Payment();
        receiver.Receive(msg);
        Assert.False(receiver.Receive(msg));
        Assert.False(receiver.Receive(msg));
        Assert.False(receiver.Receive(msg));
    }

    [Fact]
    public void TwoDistinctMessages_BothAccepted()
    {
        var (_, _, receiver) = Factory.MakeReceiver("bk-001", "bk-002");
        Assert.True(receiver.Receive(Factory.Payment("msg-001", "bk-001")));
        Assert.True(receiver.Receive(Factory.Payment("msg-002", "bk-002")));
    }
}

// ── Suite 3: WebhookReceiver — handler invocation ─────────────────────────────

public sealed class WebhookReceiver_HandlerInvocation
{
    [Fact]
    public void UniqueMessage_HandlerCalledOnce()
    {
        var (_, bookings, receiver) = Factory.MakeReceiver("bk-001");
        receiver.Receive(Factory.Payment("msg-001", "bk-001"));
        Assert.Equal(1, bookings.ConfirmCount);
    }

    [Fact]
    public void DuplicateMessage_HandlerNotCalledAgain()
    {
        var (_, bookings, receiver) = Factory.MakeReceiver("bk-001");
        var msg = Factory.Payment("msg-001", "bk-001");
        receiver.Receive(msg);
        receiver.Receive(msg);   // duplicate
        Assert.Equal(1, bookings.ConfirmCount);
    }

    [Fact]
    public void AfterHandlerRuns_MessageMarkedProcessed()
    {
        var (inbox, _, receiver) = Factory.MakeReceiver("bk-001");
        receiver.Receive(Factory.Payment("msg-001", "bk-001"));
        var record = inbox.GetAll().Single();
        Assert.Equal(InboxStatus.Processed, record.Status);
    }

    [Fact]
    public void CancellationHandler_CalledOnce()
    {
        var (_, bookings, receiver) = Factory.MakeReceiver("bk-001");
        var msg = Factory.Cancellation("msg-c-001", "bk-001");
        receiver.Receive(msg);
        Assert.Equal(1, bookings.CancelCount);
    }

    [Fact]
    public void CancellationDuplicate_HandlerNotCalledAgain()
    {
        var (_, bookings, receiver) = Factory.MakeReceiver("bk-001");
        var msg = Factory.Cancellation("msg-c-001", "bk-001");
        receiver.Receive(msg);
        receiver.Receive(msg);
        Assert.Equal(1, bookings.CancelCount);
    }
}

// ── Suite 4: BookingService ───────────────────────────────────────────────────

public sealed class BookingService_Tests
{
    [Fact]
    public void ConfirmPayment_SetsStatusToConfirmed()
    {
        var svc = new BookingService();
        svc.AddPending("bk-1", "evt-1", "a@b.ca", 99m);
        svc.ConfirmPayment("bk-1", 99m);
        Assert.Equal(Domain.BookingStatus.Confirmed, svc.Find("bk-1")!.Status);
    }

    [Fact]
    public void CancelBooking_SetsStatusToCancelled()
    {
        var svc = new BookingService();
        svc.AddPending("bk-1", "evt-1", "a@b.ca", 99m);
        svc.CancelBooking("bk-1", "Reason");
        Assert.Equal(Domain.BookingStatus.Cancelled, svc.Find("bk-1")!.Status);
    }

    [Fact]
    public void ConfirmPayment_UnknownBooking_Throws()
    {
        var svc = new BookingService();
        Assert.Throws<KeyNotFoundException>(() => svc.ConfirmPayment("missing", 99m));
    }

    [Fact]
    public void MultipleBookings_ConfirmedIndependently()
    {
        var svc = new BookingService();
        svc.AddPending("bk-1", "evt-1", "a@b.ca", 99m);
        svc.AddPending("bk-2", "evt-1", "b@b.ca", 99m);
        svc.ConfirmPayment("bk-1", 99m);
        Assert.Equal(Domain.BookingStatus.Confirmed, svc.Find("bk-1")!.Status);
        Assert.Equal(Domain.BookingStatus.Pending,   svc.Find("bk-2")!.Status);
    }
}

// ── Suite 5: Integration ──────────────────────────────────────────────────────

public sealed class Integration_Tests
{
    [Fact]
    public void FullFlow_PaymentConfirmed_BookingConfirmed()
    {
        var (inbox, bookings, receiver) = Factory.MakeReceiver("bk-001");
        receiver.Receive(Factory.Payment("msg-001", "bk-001", 189.99m));

        Assert.Equal(Domain.BookingStatus.Confirmed, bookings.Find("bk-001")!.Status);
        Assert.Equal(InboxStatus.Processed, inbox.GetAll().Single().Status);
    }

    [Fact]
    public void SameMessageDeliveredThreeTimes_ProcessedExactlyOnce()
    {
        var (inbox, bookings, receiver) = Factory.MakeReceiver("bk-001");
        var msg = Factory.Payment("msg-001", "bk-001");
        receiver.Receive(msg);
        receiver.Receive(msg);
        receiver.Receive(msg);

        Assert.Equal(1, bookings.ConfirmCount);
        Assert.Single(inbox.GetAll());
    }

    [Fact]
    public void TwoMessageTypes_EachProcessedIndependently()
    {
        var (inbox, bookings, receiver) = Factory.MakeReceiver("bk-001", "bk-002");
        receiver.Receive(Factory.Payment("msg-pay-001", "bk-001"));
        receiver.Receive(Factory.Cancellation("msg-cancel-001", "bk-002"));

        Assert.Equal(Domain.BookingStatus.Confirmed,  bookings.Find("bk-001")!.Status);
        Assert.Equal(Domain.BookingStatus.Cancelled,  bookings.Find("bk-002")!.Status);
        Assert.Equal(2, inbox.GetAll().Count);
        Assert.All(inbox.GetAll(), m => Assert.Equal(InboxStatus.Processed, m.Status));
    }

    [Fact]
    public void InboxContainsOneRecordPerUniqueMessageId_RegardlessOfRetries()
    {
        var (inbox, _, receiver) = Factory.MakeReceiver("bk-001", "bk-002");
        var msg1 = Factory.Payment("msg-001", "bk-001");
        var msg2 = Factory.Payment("msg-002", "bk-002");

        receiver.Receive(msg1); receiver.Receive(msg1); // msg1 retried once
        receiver.Receive(msg2);                          // msg2 delivered once

        Assert.Equal(2, inbox.GetAll().Count);
    }

    [Fact]
    public void MixedNewAndDuplicate_OnlyNewMessagesProcessed()
    {
        var (_, bookings, receiver) = Factory.MakeReceiver("bk-001", "bk-002", "bk-003");
        receiver.Receive(Factory.Payment("msg-001", "bk-001"));
        receiver.Receive(Factory.Payment("msg-001", "bk-001"));  // duplicate
        receiver.Receive(Factory.Payment("msg-002", "bk-002"));
        receiver.Receive(Factory.Payment("msg-003", "bk-003"));
        receiver.Receive(Factory.Payment("msg-002", "bk-002"));  // duplicate

        Assert.Equal(3, bookings.ConfirmCount);
    }
}
