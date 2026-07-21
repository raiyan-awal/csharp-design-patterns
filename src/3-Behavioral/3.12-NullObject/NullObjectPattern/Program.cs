using NullObjectPattern;
using NullObjectPattern.Loggers;
using NullObjectPattern.Notifiers;

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine("\n");
}

static void Header(string title)
{
    Console.WriteLine(new string('─', 62));
    Console.WriteLine($"  {title}");
    Console.WriteLine(new string('─', 62));
}

static Order NewOrder(string id) =>
    new(id, "Sarah Bouchard", "sarah@example.ca", "+1-416-555-0198", 149.99m);

// ─── THE PROBLEM ─────────────────────────────────────────────────────────────
Header("THE PROBLEM — null checks multiply across every method");
Console.WriteLine("""

  Without Null Object, optional dependencies are nullable and every
  method that uses them must guard against null:

    ICustomerNotifier? _notifier;
    IAuditLogger?      _logger;

    public void PlaceOrder(Order order)
    {
        if (_logger   != null) _logger.Log(order.OrderId, "PlaceOrder");
        if (_notifier != null) _notifier.NotifyOrderPlaced(order);
    }

    public void ShipOrder(Order order, string tracking)
    {
        if (_logger   != null) _logger.Log(order.OrderId, "ShipOrder");
        if (_notifier != null) _notifier.NotifyOrderShipped(order, tracking);
    }

  The same pair of null checks appears in every method and must be
  tested twice (with and without each dependency set). Null Object
  replaces all guards with objects that simply do nothing.

""");
Pause();

// ─── DEMO 1: Full setup — email + audit ──────────────────────────────────────
Header("DEMO 1 — Full setup: EmailNotifier + ConsoleAuditLogger");
Console.WriteLine("  Both real implementations are injected.");
Console.WriteLine("  Every action produces an email notification AND an audit log entry.\n");

var fullSvc = new OrderService(new EmailNotifier(), new ConsoleAuditLogger());
var order1  = NewOrder("ORD-1001");
fullSvc.PlaceOrder(order1);
fullSvc.ShipOrder(order1, "1Z999AA10123456784");
Pause();

// ─── DEMO 2: SMS + null audit ────────────────────────────────────────────────
Header("DEMO 2 — SMS notifications, no audit logging");
Console.WriteLine("  ConsoleAuditLogger replaced with NullAuditLogger.");
Console.WriteLine("  Customers receive SMS texts; audit trail is silently skipped.\n");

var smsSvc = new OrderService(new SmsNotifier(), NullAuditLogger.Instance);
var order2 = NewOrder("ORD-1002");
smsSvc.PlaceOrder(order2);
smsSvc.ShipOrder(order2, "1Z888BB20987654321");
Pause();

// ─── DEMO 3: Audit only + null notifier ──────────────────────────────────────
Header("DEMO 3 — Audit logging only, no customer notifications");
Console.WriteLine("  EmailNotifier replaced with NullCustomerNotifier.");
Console.WriteLine("  Audit trail is recorded; customers receive nothing (e.g. bulk import).\n");

var auditOnlySvc = new OrderService(NullCustomerNotifier.Instance, new ConsoleAuditLogger());
var order3       = NewOrder("ORD-1003");
auditOnlySvc.PlaceOrder(order3);
auditOnlySvc.CancelOrder(order3, "Customer request — duplicate order");
auditOnlySvc.IssueRefund(order3, 149.99m);
Pause();

// ─── DEMO 4: Silent mode — both null ─────────────────────────────────────────
Header("DEMO 4 — Silent mode: NullCustomerNotifier + NullAuditLogger");
Console.WriteLine("  Both dependencies are Null Objects.");
Console.WriteLine("  OrderService runs completely normally — neither dependency produces output.");
Console.WriteLine("  Useful for automated batch jobs, dry-run mode, or unit tests.\n");

var silentSvc = new OrderService(NullCustomerNotifier.Instance, NullAuditLogger.Instance);
var order4    = NewOrder("ORD-1004");
silentSvc.PlaceOrder(order4);
silentSvc.ShipOrder(order4, "BATCH-TRACK-001");
Console.WriteLine($"  OrderService finished — order status: {order4.Status}");
Console.WriteLine("  (No notifications sent, no audit entries written — intentionally silent.)\n");
Pause();

// ─── DEMO 5: The key insight — zero null checks ───────────────────────────────
Header("DEMO 5 — The key insight: OrderService has zero null checks");
Console.WriteLine("""

  The complete OrderService.PlaceOrder method — no guards anywhere:

    public void PlaceOrder(Order order)
    {
        order.UpdateStatus("Confirmed");
        _logger.Log(order.OrderId, "PlaceOrder", $"${order.Total:F2} — {order.CustomerName}");
        _notifier.NotifyOrderPlaced(order);
    }

  Whether _logger is ConsoleAuditLogger or NullAuditLogger, the call
  is identical. Whether _notifier is EmailNotifier, SmsNotifier, or
  NullCustomerNotifier, the call is identical.

  The Null Object guarantees a valid object is always present.
  Every code path through OrderService is the same — nothing to test twice.

""");
Pause();

Console.WriteLine("  Done.");
