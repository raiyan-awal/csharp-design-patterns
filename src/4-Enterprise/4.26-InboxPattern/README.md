# 4.26 — Inbox Pattern

## Intent

The Inbox Pattern ensures that incoming messages from external systems are processed **exactly once**, even when the delivery mechanism can deliver the same message more than once. Before processing a message, the receiver records its unique ID in an inbox table. If that ID has already been seen, the message is silently discarded as a duplicate. This guarantees idempotent consumption regardless of network retries or broker redelivery.

## The Problem It Solves

Most messaging systems and webhooks use "at-least-once" delivery: they retry until they receive an acknowledgement, which means the same message can arrive multiple times under transient failures. Without the Inbox Pattern, every retry triggers a full re-execution:

```csharp
// Without Inbox Pattern: every delivery is treated as new
public void OnPaymentWebhook(PaymentConfirmedMessage message)
{
    bookingService.ConfirmPayment(message.BookingId, message.AmountCAD);
    emailService.SendConfirmation(message.BookingId);
    // if the network blips before the ACK, the provider retries —
    // the booking is confirmed twice and two emails are sent
}
```

Problems this creates:
- **Double-processing** — a booking is confirmed multiple times; a customer receives duplicate confirmation emails.
- **Data corruption** — financial totals or inventory counts are incremented once per delivery, not once per event.
- **Side-effect duplication** — emails sent, SMS notifications triggered, and external API calls made once for each retry.

## Solution: Record Before Processing

The receiver atomically checks and records the message ID before delegating to the handler. Duplicates are detected and discarded before any business logic runs.

```csharp
public bool Receive(PaymentConfirmedMessage message)
{
    // TryRecord returns false if this ID was already seen — skip silently
    if (!inboxStore.TryRecord(message.MessageId, nameof(PaymentConfirmedMessage)))
        return false;

    paymentHandler.Handle(message);          // business logic runs exactly once
    inboxStore.MarkProcessed(message.MessageId);
    return true;
}
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Inbox store interface | `IInboxStore` | `TryRecord` (atomic check-and-insert), `MarkProcessed`, `GetAll` |
| Inbox store | `InMemoryInboxStore` | Dictionary-backed; `Lock` ensures atomicity of `TryRecord` |
| Inbox record | `InboxMessage` | Immutable record: MessageId, MessageType, ReceivedAt, ProcessedAt?, Status |
| Webhook receiver | `WebhookReceiver` | Entry point; runs check-record-handle-mark sequence |
| Message handler | `PaymentConfirmedHandler`, `BookingCancelledHandler` | Business logic; called only after uniqueness is confirmed |
| Domain service | `BookingService` | Manages bookings; `ConfirmCount` / `CancelCount` verify exactly-once processing |

## Structure

```
4.26-InboxPattern/
├── InboxPattern/
│   ├── Domain/
│   │   ├── Booking.cs          ← sealed record (Id, EventId, CustomerEmail, AmountCAD, Status)
│   │   └── BookingStatus.cs    ← Pending | Confirmed | Cancelled
│   ├── Messages/
│   │   ├── PaymentConfirmedMessage.cs   ← inbound webhook payload
│   │   └── BookingCancelledMessage.cs   ← inbound webhook payload
│   ├── Inbox/
│   │   ├── InboxMessage.cs     ← record with InboxStatus (Pending | Processed)
│   │   ├── IInboxStore.cs      ← TryRecord / MarkProcessed / GetAll
│   │   └── InMemoryInboxStore.cs ← Dictionary + Lock; TryRecord is atomic check-and-insert
│   ├── Handlers/
│   │   ├── IMessageHandler.cs           ← contravariant IMessageHandler<in TMessage>
│   │   ├── PaymentConfirmedHandler.cs   ← calls BookingService.ConfirmPayment
│   │   └── BookingCancelledHandler.cs   ← calls BookingService.CancelBooking
│   ├── Services/
│   │   └── BookingService.cs    ← domain service; ConfirmCount/CancelCount for verification
│   ├── Receiver/
│   │   └── WebhookReceiver.cs  ← check → record → handle → mark; returns false on duplicate
│   └── Program.cs
└── InboxPattern.Tests/
    └── InboxPatternTests.cs     ← 25 tests across 5 suites
```

## Key Code

### IInboxStore — atomic check-and-insert

```csharp
public interface IInboxStore
{
    // Returns true if the message is new and was recorded; false if duplicate.
    bool TryRecord(string messageId, string messageType);
    void MarkProcessed(string messageId);
    IReadOnlyList<InboxMessage> GetAll();
}
```

`TryRecord` must be atomic — the check and the insert happen in a single operation under a lock. Without that atomicity, two concurrent deliveries of the same message could both pass the check before either records the ID, causing double-processing.

### InMemoryInboxStore — lock-protected TryRecord

```csharp
public bool TryRecord(string messageId, string messageType)
{
    lock (_lock)
    {
        if (_messages.ContainsKey(messageId))
            return false;                              // duplicate — reject

        _messages[messageId] = new InboxMessage(
            messageId, messageType, DateTimeOffset.UtcNow, null, InboxStatus.Pending);
        return true;                                   // new — accepted
    }
}
```

The lock makes the check-then-insert indivisible. A concurrent second delivery will block until the first has either recorded or rejected, then find the ID already present and return false.

### WebhookReceiver — the full sequence

```csharp
public bool Receive(PaymentConfirmedMessage message)
{
    if (!inboxStore.TryRecord(message.MessageId, nameof(PaymentConfirmedMessage)))
        return false;                          // (1) duplicate detected — stop here

    paymentHandler.Handle(message);            // (2) business logic — runs exactly once
    inboxStore.MarkProcessed(message.MessageId);   // (3) mark complete
    return true;
}
```

Steps 1–3 are deliberately separate:
- Step 1 alone (before handling) means a crash between 1 and 2 leaves the message as `Pending` — a recovery job can find and retry it.
- Step 3 (after handling) confirms the message completed successfully. The next delivery attempt hits step 1 and is rejected even if step 3 has run.

## Demo Scenarios

```
1. Normal delivery       — two distinct payment webhooks processed; bookings confirmed
2. Duplicate delivery    — same message retried 3× by provider; still processed exactly once
3. Mixed interleaving    — new message + retry of old message; only new one processed
4. Cancellation message  — different message type; also deduplicated correctly
5. Inbox audit log       — all unique message IDs shown with Pending / Processed status
```

## When to Use

- Your service consumes messages from a broker or webhook provider that guarantees at-least-once delivery (RabbitMQ, Azure Service Bus, Stripe webhooks, etc.).
- The handler performs non-idempotent operations: financial updates, email sends, external API calls, inventory decrements.
- You need an audit trail of every message your service has received.
- You are implementing the consumer side of a Saga (4.19) or reacting to Domain Events (4.12) published via the Outbox Pattern (4.20).

## When NOT to Use

- The handler is already naturally idempotent (e.g., a pure `SET` to a key-value store) — the Inbox adds overhead for no benefit.
- Messages do not carry a stable unique ID that survives retries — without a reliable `MessageId`, deduplication is impossible.
- Your broker guarantees exactly-once delivery natively (rare, and even then the guarantee often only holds within the broker's own transaction boundary).

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Exactly-once processing | Business logic runs once per unique message ID, regardless of retry count |
| Audit trail | Every received message is recorded with its status; easy to inspect, replay, or alert on stuck entries |
| Handler simplicity | Handlers do not need to defend themselves against duplicates; the receiver does it for them |
| Failure recovery | A message recorded as Pending but not yet Processed can be retried by a recovery job |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Storage overhead | Every unique message ID must be persisted; the inbox table grows with traffic |
| Stuck messages | If the handler throws after TryRecord, the entry stays Pending; a recovery job is needed to detect and retry it |
| No cross-process atomicity | In-memory TryRecord is not safe across multiple processes; production use requires a database with a unique constraint on MessageId |
| MessageId trust | Deduplication is only as reliable as the uniqueness of the incoming MessageId; a provider that reuses IDs for distinct events will cause missed processing |

## Related Patterns

- **Outbox Pattern (4.20)** — the sender-side complement: guarantees at-least-once publishing; the Inbox Pattern is the consumer-side complement that converts at-least-once into exactly-once.
- **Saga Pattern (4.19)** — Sagas coordinate multi-step distributed workflows; each step's message is typically deduplicated with an Inbox to prevent partial re-execution on retry.
- **Domain Event (4.12)** — domain events raised inside an aggregate are typically published via the Outbox and consumed via the Inbox, completing the reliable event pipeline.
- **Idempotency Key** — the `MessageId` in this pattern is an application of the Idempotency Key concept: a stable, client-supplied identifier that makes an operation safe to repeat.

## Running the Demo

```bash
cd src/4-Enterprise/4.26-InboxPattern/InboxPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.26-InboxPattern/InboxPattern.Tests
dotnet test
```
