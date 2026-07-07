using ObserverPattern;

// ============================================================
// OBSERVER PATTERN — DEMO
// ============================================================
// An Order (Subject) notifies five observers whenever its status
// changes. Each observer reacts independently — the Order never
// knows what they do with the notification.
// ============================================================

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine("\n");
}

static void Divider(string title)
{
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine(title);
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine();
}

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║       OBSERVER PATTERN — Order Tracking System       ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("Subject:   Order — notifies all observers on every status change");
Console.WriteLine("Observers:");
Console.WriteLine("  • CustomerNotifier  — push notification to customer");
Console.WriteLine("  • WarehouseSystem   — pick, pack, dispatch, inventory");
Console.WriteLine("  • EmailService      — confirmation emails");
Console.WriteLine("  • AnalyticsDashboard — records every status transition");
Console.WriteLine("  • InvoiceService    — generates invoice on delivery");
Console.WriteLine();
Console.WriteLine("Order never references any observer type — only IOrderObserver.");

Pause();

// ── DEMO 1: Full order lifecycle ──────────────────────────────
Divider("DEMO 1 — Full order lifecycle (Placed → Delivered)");

var order     = new Order("ORD-1001", "CUST-42", 149.99m);
var customer  = new CustomerNotifier();
var warehouse = new WarehouseSystem();
var email     = new EmailService();
var analytics = new AnalyticsDashboard();
var invoice   = new InvoiceService();

order.Subscribe(customer);
order.Subscribe(warehouse);
order.Subscribe(email);
order.Subscribe(analytics);
order.Subscribe(invoice);

Console.WriteLine("5 observers subscribed. Advancing order through all statuses:\n");

order.UpdateStatus(OrderStatus.Processing);
order.UpdateStatus(OrderStatus.Shipped);
order.UpdateStatus(OrderStatus.Delivered);

Console.WriteLine();
Console.WriteLine($"  Invoice generated : {invoice.InvoiceGenerated}  (£{invoice.InvoiceAmount:F2})");
Console.WriteLine($"  Analytics transitions recorded: {analytics.Transitions.Count}");
Console.WriteLine($"  Emails sent: {email.SentEmails.Count}");

Pause();

// ── DEMO 2: Duplicate status — no notification ────────────────
Divider("DEMO 2 — Same status twice → observers not notified");

var order2 = new Order("ORD-1002", "CUST-43", 29.99m);
var analytics2 = new AnalyticsDashboard();
order2.Subscribe(analytics2);

Console.WriteLine("Updating to Processing twice:");
order2.UpdateStatus(OrderStatus.Processing);
order2.UpdateStatus(OrderStatus.Processing); // no-op

Console.WriteLine();
Console.WriteLine($"  Analytics transitions: {analytics2.Transitions.Count}  (expected 1 — duplicate ignored)");

Pause();

// ── DEMO 3: Unsubscribe mid-lifecycle ─────────────────────────
Divider("DEMO 3 — Unsubscribe: CustomerNotifier removed after Shipped");

var order3    = new Order("ORD-1003", "CUST-44", 79.99m);
var customer3 = new CustomerNotifier();
var email3    = new EmailService();
order3.Subscribe(customer3);
order3.Subscribe(email3);

Console.WriteLine("Processing and Shipped — CustomerNotifier is subscribed:");
order3.UpdateStatus(OrderStatus.Processing);
order3.UpdateStatus(OrderStatus.Shipped);

Console.WriteLine();
Console.WriteLine("Unsubscribing CustomerNotifier...");
order3.Unsubscribe(customer3);

Console.WriteLine();
Console.WriteLine("Delivered — CustomerNotifier should NOT fire:");
order3.UpdateStatus(OrderStatus.Delivered);

Console.WriteLine();
Console.WriteLine($"  CustomerNotifier notifications: {customer3.Notifications.Count}  (expected 2 — Processing + Shipped only)");
Console.WriteLine($"  EmailService emails: {email3.SentEmails.Count}  (still receiving — Shipped + Delivered)");

Pause();

// ── DEMO 4: Late subscriber ───────────────────────────────────
Divider("DEMO 4 — Late subscriber only receives events from subscribe point onward");

var order4    = new Order("ORD-1004", "CUST-45", 199.99m);
var earlyObs  = new AnalyticsDashboard();
var lateObs   = new AnalyticsDashboard();

order4.Subscribe(earlyObs);
order4.UpdateStatus(OrderStatus.Processing); // earlyObs sees this

order4.Subscribe(lateObs);                   // joins after first change
order4.UpdateStatus(OrderStatus.Shipped);    // both see this
order4.UpdateStatus(OrderStatus.Delivered);  // both see this

Console.WriteLine();
Console.WriteLine($"  Early observer transitions: {earlyObs.Transitions.Count}  (expected 3)");
Console.WriteLine($"  Late observer transitions:  {lateObs.Transitions.Count}  (expected 2 — missed Processing)");

Pause();

// ── DEMO 5: Cancellation ──────────────────────────────────────
Divider("DEMO 5 — Cancellation path");

var order5    = new Order("ORD-1005", "CUST-46", 59.99m);
var customer5 = new CustomerNotifier();
var warehouse5 = new WarehouseSystem();
order5.Subscribe(customer5);
order5.Subscribe(warehouse5);

order5.UpdateStatus(OrderStatus.Processing);
order5.UpdateStatus(OrderStatus.Cancelled);

Console.WriteLine();
Console.WriteLine($"  Warehouse picking: {warehouse5.IsPickingOrder}  (cancelled — items returned to shelf)");
Console.WriteLine($"  Customer push messages: {customer5.Notifications.Count}");
foreach (var n in customer5.Notifications)
    Console.WriteLine($"    → {n}");

Pause();

// ── DEMO 6: C# event keyword — the built-in Observer ─────────
Divider("DEMO 6 — C# event keyword: the language-native Observer pattern");

Console.WriteLine("C# 'event' IS the Observer pattern built into the language.");
Console.WriteLine("Instead of Subscribe/Unsubscribe, you use += and -=.\n");

// Inline demonstration using Action delegate events
var stockPrice   = 100.0m;
var priceChanged = default(Action<decimal, decimal>?); // (oldPrice, newPrice)

void LogPriceChange(decimal old, decimal next)
    => Console.WriteLine($"  [Logger]   Price changed: £{old} → £{next}");

void AlertTrader(decimal old, decimal next)
{
    if (next < old * 0.95m)
        Console.WriteLine($"  [Trader]   ALERT: Price dropped >5%%! Selling at £{next}");
    else
        Console.WriteLine($"  [Trader]   Monitoring... (£{next})");
}

priceChanged += LogPriceChange;
priceChanged += AlertTrader;

void SetPrice(decimal newPrice)
{
    var old = stockPrice;
    stockPrice = newPrice;
    priceChanged?.Invoke(old, newPrice);
}

Console.WriteLine("Subscribing Logger and Trader via +=:");
SetPrice(98.0m);
SetPrice(92.0m); // >5% drop — triggers trader alert

Console.WriteLine();
Console.WriteLine("Unsubscribing Logger via -=:");
priceChanged -= LogPriceChange;
SetPrice(91.0m); // only Trader fires

Console.WriteLine();
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("Summary:");
Console.WriteLine("  • IOrderSubject     — Subscribe / Unsubscribe");
Console.WriteLine("  • IOrderObserver    — OnOrderUpdated(order, previousStatus)");
Console.WriteLine("  • Order             — Subject; notifies all observers on UpdateStatus()");
Console.WriteLine("  • 5 concrete observers — each reacts to the same event differently");
Console.WriteLine("  • Same status twice → no notification (guard in UpdateStatus)");
Console.WriteLine("  • ToList() snapshot → safe if observer unsubscribes during notification");
Console.WriteLine("  • C# event keyword  → language-native Observer (+=, -=, ?.Invoke)");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
