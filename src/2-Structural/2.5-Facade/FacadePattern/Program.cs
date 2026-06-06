using FacadePattern;

// ============================================================
// FACADE PATTERN — DEMO
// ============================================================
// Shows how OrderFacade hides the complexity of coordinating
// five subsystems (inventory, payment, shipping, notifications,
// audit) behind two simple methods: PlaceOrder and CancelOrder.
// ============================================================

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine("\n");
}

static void PrintSeparator(string title)
{
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine(title);
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine();
}

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║         FACADE PATTERN — Order Processing            ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("OrderFacade hides five subsystems behind two methods.");
Console.WriteLine("The client calls PlaceOrder() — the Facade handles");
Console.WriteLine("sequencing, compensation on failure, and audit logging.");

Pause();

// ── Build subsystems directly so the demo can inspect state ──
var inventory = new InventoryService(new Dictionary<string, int>
{
    ["LAPTOP-001"] = 5,
    ["MOUSE-002"]  = 12,
    ["HDMI-003"]   = 0,   // out of stock
});

var payment       = new PaymentService();
var shipping      = new ShippingService();
var notifications = new NotificationService();
var audit         = new AuditLogger();

var facade = new OrderFacade(inventory, payment, shipping, notifications, audit);

// ── Shared order lines ────────────────────────────────────────
var laptop = new OrderLine("LAPTOP-001", "Pro Laptop 15\"",  1, 1_299.99m);
var mouse  = new OrderLine("MOUSE-002",  "Wireless Mouse",   2,    29.99m);
var hdmi   = new OrderLine("HDMI-003",   "HDMI Cable 2m",    1,     9.99m);

// ── DEMO 1: PlaceOrder — happy path ──────────────────────────
PrintSeparator("DEMO 1 — PlaceOrder: happy path (all subsystems cooperate)");

var order1 = new Order(
    OrderId:         "ORD-1001",
    CustomerId:      "CUST-42",
    CustomerEmail:   "alice@example.com",
    ShippingAddress: "10 Maple Street, Springfield",
    CardToken:       "card-visa-4242",
    Lines:           [laptop, mouse]);

var result1 = facade.PlaceOrder(order1);
Console.WriteLine();
Console.WriteLine($"Result     : {(result1.Success ? "SUCCESS ✓" : "FAILED ✗")}");
Console.WriteLine($"Message    : {result1.Message}");
Console.WriteLine($"Tracking   : {result1.TrackingNumber}");
Console.WriteLine($"Transaction: {result1.TransactionId}");

Pause();

// ── DEMO 2: PlaceOrder — out of stock ────────────────────────
PrintSeparator("DEMO 2 — PlaceOrder: item out of stock (no charge, no shipment)");

var order2 = new Order(
    OrderId:         "ORD-1002",
    CustomerId:      "CUST-43",
    CustomerEmail:   "bob@example.com",
    ShippingAddress: "5 Oak Avenue, Shelbyville",
    CardToken:       "card-mc-5555",
    Lines:           [mouse, hdmi]);    // HDMI is out of stock

var result2 = facade.PlaceOrder(order2);
Console.WriteLine();
Console.WriteLine($"Result : {(result2.Success ? "SUCCESS ✓" : "FAILED ✗")}");
Console.WriteLine($"Message: {result2.Message}");

Pause();

// ── DEMO 3: PlaceOrder — payment declined ────────────────────
PrintSeparator("DEMO 3 — PlaceOrder: payment declined (reserved stock is released)");

Console.WriteLine($"Laptop stock BEFORE: {inventory.GetStock("LAPTOP-001")}");
Console.WriteLine();

var order3 = new Order(
    OrderId:         "ORD-1003",
    CustomerId:      "CUST-44",
    CustomerEmail:   "carol@example.com",
    ShippingAddress: "22 Pine Road, Capital City",
    CardToken:       "fail-declined",  // fail- prefix is always declined
    Lines:           [laptop]);

var result3 = facade.PlaceOrder(order3);
Console.WriteLine();
Console.WriteLine($"Result : {(result3.Success ? "SUCCESS ✓" : "FAILED ✗")}");
Console.WriteLine($"Message: {result3.Message}");
Console.WriteLine();
Console.WriteLine($"Laptop stock AFTER (should be unchanged): {inventory.GetStock("LAPTOP-001")}");

Pause();

// ── DEMO 4: CancelOrder ───────────────────────────────────────
PrintSeparator("DEMO 4 — CancelOrder: cancel ORD-1001 (shipment + refund + notify)");

Console.WriteLine($"Laptop stock before cancel: {inventory.GetStock("LAPTOP-001")}");
Console.WriteLine();

var result4 = facade.CancelOrder(order1, result1.TrackingNumber!, result1.TransactionId!);
Console.WriteLine();
Console.WriteLine($"Result : {(result4.Success ? "SUCCESS ✓" : "FAILED ✗")}");
Console.WriteLine($"Message: {result4.Message}");
Console.WriteLine();
Console.WriteLine($"Laptop stock after cancel (restored): {inventory.GetStock("LAPTOP-001")}");

Pause();

// ── DEMO 5: Without the Facade ───────────────────────────────
PrintSeparator("DEMO 5 — Without Facade: what every caller would have to manage");

Console.WriteLine("Every controller, service, or job that places an order");
Console.WriteLine("would have to implement all of this itself:");
Console.WriteLine();
Console.WriteLine("  foreach (line in order.Lines)");
Console.WriteLine("      if (!inventory.IsAvailable(line.ProductId, line.Quantity))");
Console.WriteLine("          return error;");
Console.WriteLine();
Console.WriteLine("  foreach (line in order.Lines)");
Console.WriteLine("      inventory.Reserve(line.ProductId, line.Quantity);");
Console.WriteLine();
Console.WriteLine("  var charge = payment.Charge(order.CardToken, total);");
Console.WriteLine("  if (!charge.Success) {");
Console.WriteLine("      foreach (line) inventory.Release(...);  // ← compensation");
Console.WriteLine("      return error;");
Console.WriteLine("  }");
Console.WriteLine();
Console.WriteLine("  var tracking = shipping.CreateShipment(address, lines);");
Console.WriteLine("  notifications.SendOrderConfirmation(email, orderId, tracking);");
Console.WriteLine("  audit.Log(\"PlaceOrder\", ...);");
Console.WriteLine();
Console.WriteLine("That's 5 subsystem APIs, 7 ordered steps, and compensation");
Console.WriteLine("logic — duplicated in every place that touches an order.");
Console.WriteLine();
Console.WriteLine("With Facade:");
Console.WriteLine();
Console.WriteLine("  var result = facade.PlaceOrder(order);");
Console.WriteLine();
Console.WriteLine("One call. Subsystems can change without any caller updating.");

Console.WriteLine();
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("Summary:");
Console.WriteLine("  • 5 subsystems — each focused on one concern");
Console.WriteLine("  • OrderFacade — sequences steps and compensates on failure");
Console.WriteLine("  • Client calls PlaceOrder() / CancelOrder() — nothing else");
Console.WriteLine("  • Subsystems remain accessible directly for advanced use");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
