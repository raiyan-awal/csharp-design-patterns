using InboxPattern.Handlers;
using InboxPattern.Inbox;
using InboxPattern.Messages;
using InboxPattern.Receiver;
using InboxPattern.Services;

Console.WriteLine("=== 4.26 Inbox Pattern — Maple Events Ticketing ===");
Console.WriteLine();

// ── Setup ────────────────────────────────────────────────────────────────────

var inboxStore   = new InMemoryInboxStore();
var bookings     = new BookingService();
var payHandler   = new PaymentConfirmedHandler(bookings);
var cancelHandler = new BookingCancelledHandler(bookings);
var receiver     = new WebhookReceiver(inboxStore, payHandler, cancelHandler);

// Seed pending bookings (created when customers reserved seats)
bookings.AddPending("bk-001", "evt-toronto-001", "alice@example.ca",   189.99m);
bookings.AddPending("bk-002", "evt-toronto-001", "bob@example.ca",     189.99m);
bookings.AddPending("bk-003", "evt-toronto-001", "charlie@example.ca", 249.99m);
bookings.AddPending("bk-004", "evt-toronto-001", "diana@example.ca",   189.99m);

Console.WriteLine("Event: The Weeknd at Scotiabank Arena, Toronto");
Console.WriteLine($"Pending bookings: {bookings.GetAll().Count}");
Console.WriteLine();

// ── Section 1: Normal delivery ───────────────────────────────────────────────

Console.WriteLine("── 1. Normal Delivery — First-Time Messages ──");
Console.WriteLine();

var msg1 = new PaymentConfirmedMessage("msg-pay-001", "bk-001", 189.99m, DateTimeOffset.UtcNow);
var msg2 = new PaymentConfirmedMessage("msg-pay-002", "bk-002", 189.99m, DateTimeOffset.UtcNow);

bool r1 = receiver.Receive(msg1);
bool r2 = receiver.Receive(msg2);

Console.WriteLine($"  msg-pay-001 accepted: {r1}   msg-pay-002 accepted: {r2}");
Console.WriteLine($"  Confirmations processed: {bookings.ConfirmCount}");

Pause();

// ── Section 2: Duplicate delivery ────────────────────────────────────────────

Console.WriteLine("── 2. Duplicate Delivery — Payment Provider Retries ──");
Console.WriteLine();

Console.WriteLine("  Payment provider retries msg-pay-001 three more times...");
bool dup1 = receiver.Receive(msg1);   // duplicate
bool dup2 = receiver.Receive(msg1);   // duplicate
bool dup3 = receiver.Receive(msg1);   // duplicate

Console.WriteLine($"  Duplicates accepted: {dup1}, {dup2}, {dup3}  (all should be false)");
Console.WriteLine($"  Confirmations processed total: {bookings.ConfirmCount}  (still 2 — no double-processing)");
Console.WriteLine($"  bk-001 status: {bookings.Find("bk-001")!.Status}");

Pause();

// ── Section 3: Concurrent unique + duplicate mix ──────────────────────────────

Console.WriteLine("── 3. Mixed Delivery — New Messages and Retries Interleaved ──");
Console.WriteLine();

var msg3 = new PaymentConfirmedMessage("msg-pay-003", "bk-003", 249.99m, DateTimeOffset.UtcNow);

Console.WriteLine("  Processing new msg-pay-003, then retrying msg-pay-002...");
receiver.Receive(msg3);          // new — processed
receiver.Receive(msg2);          // duplicate — skipped

Console.WriteLine($"  Confirmations processed total: {bookings.ConfirmCount}  (should be 3)");

Pause();

// ── Section 4: Cancellation message ──────────────────────────────────────────

Console.WriteLine("── 4. Booking Cancellation — Different Message Type ──");
Console.WriteLine();

var cancelMsg = new BookingCancelledMessage(
    "msg-cancel-004", "bk-004", "Customer requested refund", DateTimeOffset.UtcNow);

receiver.Receive(cancelMsg);
Console.WriteLine($"  bk-004 status: {bookings.Find("bk-004")!.Status}");

Console.WriteLine("  Retrying cancellation message (provider resent)...");
receiver.Receive(cancelMsg);   // duplicate

Console.WriteLine($"  Cancellations processed: {bookings.CancelCount}  (should be 1)");

Pause();

// ── Section 5: Inbox audit log ────────────────────────────────────────────────

Console.WriteLine("── 5. Inbox Audit Log ──");
Console.WriteLine();

var all = inboxStore.GetAll();
Console.WriteLine($"  Total inbox records: {all.Count}  (unique message IDs only)");
Console.WriteLine();
Console.WriteLine($"  {"MessageId",-20} {"Type",-30} {"Status"}");
Console.WriteLine($"  {new string('-', 65)}");
foreach (var msg in all.OrderBy(m => m.ReceivedAt))
    Console.WriteLine($"  {msg.MessageId,-20} {msg.MessageType,-30} {msg.Status}");

Console.WriteLine();
Console.WriteLine($"  Processed : {all.Count(m => m.Status == InboxStatus.Processed)}");
Console.WriteLine($"  Pending   : {all.Count(m => m.Status == InboxStatus.Pending)}");
Console.WriteLine();
Console.WriteLine("=== End of Demo ===");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
