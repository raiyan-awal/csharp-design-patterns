using OutboxPattern.Core;
using OutboxPattern.Domain;
using OutboxPattern.Infrastructure;
using OutboxPattern.Services;

Console.WriteLine("=== Maple Shop — Outbox Pattern Demo ===\n");

var orderRepo    = new InMemoryOrderRepository();
var outboxStore  = new InMemoryOutboxStore();
var orderService = new OrderService(orderRepo, outboxStore);

var emailHandler     = new SimulatedEmailHandler();
var inventoryHandler = new SimulatedInventoryHandler();

OutboxRelay BuildRelay(Action<string>? onPublished = null) =>
    new(outboxStore,
        publish: msg =>
        {
            emailHandler.Handle(msg);
            inventoryHandler.Handle(msg);
        },
        onPublished: onPublished);

// ── Section 1: Normal Order Flow ─────────────────────────────────────────────
Console.WriteLine("--- Section 1: Normal Order Flow ---");

var order1 = orderService.PlaceOrder(
    "CUST-001", "Alice Tremblay",
    [
        new OrderItem("Maple Syrup (500 mL)", 2, 14.99m),
        new OrderItem("Poutine Kit",          1, 22.49m),
    ]);

Console.WriteLine($"  Order placed  : {order1.Id}");
Console.WriteLine($"  Customer      : {order1.CustomerName}");
Console.WriteLine($"  Total         : ${order1.TotalCAD:N2} CAD");
Console.WriteLine($"  Outbox pending: {outboxStore.GetUnprocessed().Count}");

Console.WriteLine();
Console.WriteLine("  [Running relay...]");

var relay1 = BuildRelay(onPublished: name => Console.WriteLine($"  ✓ Published: {name}"));
var count1 = relay1.ProcessPending();

Console.WriteLine();
Console.WriteLine($"  Relay processed  : {count1} message(s)");
Console.WriteLine($"  Outbox pending   : {outboxStore.GetUnprocessed().Count}");
Console.WriteLine($"  Email handler    : {emailHandler.ReceivedEvents.Last()}");
Console.WriteLine($"  Inventory handler: {inventoryHandler.ReceivedEvents.Last()}");

Pause();

// ── Section 2: Broker Failure and Retry ──────────────────────────────────────
Console.WriteLine("--- Section 2: Broker Failure — Retry on Next Run ---");

var order2 = orderService.PlaceOrder(
    "CUST-002", "Ben Kowalczyk",
    [new OrderItem("Hockey Stick", 1, 189.99m)]);

Console.WriteLine($"  Order placed  : {order2.Id}");
Console.WriteLine($"  Outbox pending: {outboxStore.GetUnprocessed().Count}");

inventoryHandler.FailOnNextCall();

Console.WriteLine();
Console.WriteLine("  [First relay run — inventory service unavailable...]");
var relay2a = BuildRelay();
var count2a = relay2a.ProcessPending();

Console.WriteLine($"  Processed     : {count2a} (0 expected — publish threw)");
Console.WriteLine($"  Outbox pending: {outboxStore.GetUnprocessed().Count} (message still queued)");

Console.WriteLine();
Console.WriteLine("  [Second relay run — inventory service recovered...]");
var relay2b = BuildRelay(onPublished: name => Console.WriteLine($"  ✓ Published: {name}"));
var count2b = relay2b.ProcessPending();

Console.WriteLine($"  Processed     : {count2b}");
Console.WriteLine($"  Outbox pending: {outboxStore.GetUnprocessed().Count}");

Pause();

// ── Section 3: Atomicity — Order Save Fails ───────────────────────────────────
Console.WriteLine("--- Section 3: Atomicity — No Orphaned Outbox Entry on Failure ---");

orderRepo.FailOnNextSave();
var pendingBefore = outboxStore.GetUnprocessed().Count;

Console.WriteLine("  [Simulating database write failure...]");
try
{
    orderService.PlaceOrder("CUST-003", "Sophie Bouchard",
        [new OrderItem("Canoe Paddle", 1, 74.99m)]);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"  ✗ Order save failed: {ex.Message}");
}

var pendingAfter = outboxStore.GetUnprocessed().Count;
Console.WriteLine($"  Outbox pending before: {pendingBefore}");
Console.WriteLine($"  Outbox pending after : {pendingAfter}");
Console.WriteLine($"  {(pendingAfter == pendingBefore ? "✓ No orphaned outbox entry — atomicity preserved." : "✗ Orphaned entry found.")}");

Pause();

// ── Section 4: Batch — Multiple Orders Processed in One Relay Run ────────────
Console.WriteLine("--- Section 4: Batch Processing — Multiple Orders ---");

orderService.PlaceOrder("CUST-004", "Marcus Osei",
    [new OrderItem("Toque", 3, 24.99m)]);
orderService.PlaceOrder("CUST-005", "Liu Yang",
    [new OrderItem("Ice Skates", 1, 149.99m)]);
orderService.PlaceOrder("CUST-006", "Priya Sharma",
    [new OrderItem("Flannel Shirt", 2, 59.99m)]);

Console.WriteLine($"  Orders placed : 3");
Console.WriteLine($"  Outbox pending: {outboxStore.GetUnprocessed().Count}");

Console.WriteLine();
Console.WriteLine("  [Running relay...]");
var relay4   = BuildRelay(onPublished: name => Console.WriteLine($"  ✓ Published: {name}"));
var count4   = relay4.ProcessPending();

Console.WriteLine();
Console.WriteLine($"  Relay processed: {count4} message(s)");
Console.WriteLine($"  Outbox pending : {outboxStore.GetUnprocessed().Count}");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
