using Microsoft.Extensions.DependencyInjection;
using DependencyInjectionPattern;

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}

static void Header(string title)
{
    Console.WriteLine(new string('─', 62));
    Console.WriteLine($"  {title}");
    Console.WriteLine(new string('─', 62));
}

static void PrintOrder(OrderSummary order)
{
    Console.WriteLine($"  Order ID  : {order.OrderId}");
    Console.WriteLine($"  Placed at : {order.PlacedAt:yyyy-MM-dd HH:mm:ss} UTC");
    Console.WriteLine();
    foreach (var item in order.Items)
        Console.WriteLine($"    {item.Product.Name,-32} x{item.Quantity}  ${item.Subtotal:F2}");
    Console.WriteLine();
    Console.WriteLine($"  Subtotal  : ${order.Subtotal:F2}");
    Console.WriteLine($"  HST       : ${order.HstAmount:F2}");
    Console.WriteLine($"  Total     : ${order.Total:F2}");
}

Console.WriteLine("=== Dependency Injection Pattern — Maple Leaf Electronics ===\n");

// ─── THE PROBLEM ──────────────────────────────────────────────────────────────
Header("THE PROBLEM — hard-coded dependencies");
Console.WriteLine("""

  Without DI, each class creates its own dependencies with `new`:

    public class CheckoutService
    {
        private readonly InventoryService  _inventory = new InventoryService();
        private readonly ShoppingCart      _cart      = new ShoppingCart();
        private readonly HstCalculator     _tax       = new HstCalculator();
    }

  Problems:
  - Cannot swap InventoryService for a test double — the type is hard-coded
  - Every CheckoutService creates its own InventoryService — no sharing,
    the expensive catalogue load happens once per service instance
  - Lifetime is uncontrolled — nothing stops two services from getting
    different InventoryService instances when they should share one
  - To change the tax provider, you must edit CheckoutService itself

  DI inverts this: each class declares what it needs (interfaces),
  and the container creates and injects the right concrete instances.

""");
Pause();

// ─── Build the container ──────────────────────────────────────────────────────
var services = new ServiceCollection();
services.AddCheckoutServices();
var provider = services.BuildServiceProvider();

// ─── DEMO 1: Basic resolution ─────────────────────────────────────────────────
Header("DEMO 1 — Resolving services from the container");
Console.WriteLine();

var inventory = provider.GetRequiredService<IInventoryService>();
Console.WriteLine("  Available products:");
foreach (var p in inventory.GetAll())
    Console.WriteLine($"    [{p.Id}] {p.Name,-32} ${p.Price:F2}  ({p.Category})");

Console.WriteLine();
Console.WriteLine("  Container resolved IInventoryService → InventoryService");
Console.WriteLine($"  Instance ID: {inventory.InstanceId}");
Pause();

// ─── DEMO 2: Singleton lifetime ───────────────────────────────────────────────
Header("DEMO 2 — Singleton: one instance for the container's lifetime");
Console.WriteLine();

var inv1 = provider.GetRequiredService<IInventoryService>();
var inv2 = provider.GetRequiredService<IInventoryService>();

Console.WriteLine($"  First  resolution  → {inv1.InstanceId}");
Console.WriteLine($"  Second resolution  → {inv2.InstanceId}");
Console.WriteLine($"  Same instance?     → {inv1.InstanceId == inv2.InstanceId}");
Console.WriteLine();
Console.WriteLine("  InventoryService is Singleton: the catalogue loads once.");
Console.WriteLine("  Every consumer in the app shares the same instance.");
Pause();

// ─── DEMO 3: Scoped lifetime ──────────────────────────────────────────────────
Header("DEMO 3 — Scoped: one instance per scope (session)");
Console.WriteLine();

Guid scopeACartId, scopeBCartId;

using (var scopeA = provider.CreateScope())
{
    var cartA1 = scopeA.ServiceProvider.GetRequiredService<IShoppingCart>();
    var cartA2 = scopeA.ServiceProvider.GetRequiredService<IShoppingCart>();
    scopeACartId = cartA1.InstanceId;

    Console.WriteLine($"  Scope A — resolution 1: {cartA1.InstanceId}");
    Console.WriteLine($"  Scope A — resolution 2: {cartA2.InstanceId}");
    Console.WriteLine($"  Same within scope A?    {cartA1.InstanceId == cartA2.InstanceId}");
}

using (var scopeB = provider.CreateScope())
{
    var cartB = scopeB.ServiceProvider.GetRequiredService<IShoppingCart>();
    scopeBCartId = cartB.InstanceId;
    Console.WriteLine();
    Console.WriteLine($"  Scope B — resolution 1: {cartB.InstanceId}");
}

Console.WriteLine();
Console.WriteLine($"  Scope A vs Scope B same? {scopeACartId == scopeBCartId}");
Console.WriteLine();
Console.WriteLine("  ShoppingCart is Scoped: each customer session gets its own cart.");
Console.WriteLine("  Two requests for IShoppingCart within one scope return the same cart.");
Console.WriteLine("  A new scope (new session) gets a fresh cart.");
Pause();

// ─── DEMO 4: Transient lifetime ───────────────────────────────────────────────
Header("DEMO 4 — Transient: new instance every time");
Console.WriteLine();

using var scope = provider.CreateScope();
var tax1 = scope.ServiceProvider.GetRequiredService<IHstCalculator>();
var tax2 = scope.ServiceProvider.GetRequiredService<IHstCalculator>();

Console.WriteLine($"  First  resolution → {tax1.InstanceId}");
Console.WriteLine($"  Second resolution → {tax2.InstanceId}");
Console.WriteLine($"  Same instance?    → {tax1.InstanceId == tax2.InstanceId}");
Console.WriteLine();
Console.WriteLine("  HstCalculator is Transient: stateless, cheap to create.");
Console.WriteLine("  Each consumer gets its own instance — no shared state to worry about.");
Console.WriteLine();
Console.WriteLine("  HST rates by province:");
foreach (var province in new[] { "ON", "BC", "AB", "QC", "NS" })
    Console.WriteLine($"    {province}  {tax1.Rate(province) * 100:F3}%");
Pause();

// ─── DEMO 5: Full checkout flow ───────────────────────────────────────────────
Header("DEMO 5 — Full checkout: CheckoutService with injected dependencies");
Console.WriteLine();

using var checkoutScope = provider.CreateScope();
var sp       = checkoutScope.ServiceProvider;
var cart     = sp.GetRequiredService<IShoppingCart>();
var checkout = sp.GetRequiredService<ICheckoutService>();
var inv      = sp.GetRequiredService<IInventoryService>();

var speaker = inv.GetById(1)!;
var boots   = inv.GetById(2)!;
var tote    = inv.GetById(3)!;

cart.Add(speaker, 1);
cart.Add(boots,   1);
cart.Add(tote,    2);

Console.WriteLine("  Cart contents:");
foreach (var item in cart.Items)
    Console.WriteLine($"    {item.Product.Name,-32} x{item.Quantity}  ${item.Subtotal:F2}");
Console.WriteLine($"  Subtotal: ${cart.Subtotal:F2}");
Console.WriteLine();

var order = checkout.Checkout(province: "ON");
Console.WriteLine("  Order placed (Ontario — 13% HST):");
PrintOrder(order!);
Pause();

// ─── DEMO 6: Swapping implementations ────────────────────────────────────────
Header("DEMO 6 — Swapping implementations without changing consumers");
Console.WriteLine();

Console.WriteLine("  Scenario: replace HstCalculator with a zero-rate stub for testing.");
Console.WriteLine();
Console.WriteLine("  Before (real container):");
Console.WriteLine("    services.AddTransient<IHstCalculator, HstCalculator>();");
Console.WriteLine();
Console.WriteLine("  After (test container):");
Console.WriteLine("    services.AddTransient<IHstCalculator, ZeroRateCalculator>();");
Console.WriteLine();

// Demonstrate the swap inline
var testServices = new ServiceCollection();
testServices.AddSingleton<IInventoryService, InventoryService>();
testServices.AddScoped<IShoppingCart, ShoppingCart>();
testServices.AddScoped<ICheckoutService, CheckoutService>();
testServices.AddTransient<IHstCalculator, ZeroRateCalculator>(); // ← swapped

using var testProvider = testServices.BuildServiceProvider();
using var testScope    = testProvider.CreateScope();

var testCart     = testScope.ServiceProvider.GetRequiredService<IShoppingCart>();
var testCheckout = testScope.ServiceProvider.GetRequiredService<ICheckoutService>();
var testInv      = testScope.ServiceProvider.GetRequiredService<IInventoryService>();

testCart.Add(testInv.GetById(4)!, 1); // North Coast Wool Sweater — $119.99

var testOrder = testCheckout.Checkout(province: "ON");
Console.WriteLine("  Order with ZeroRateCalculator (HST should be $0.00):");
PrintOrder(testOrder!);
Console.WriteLine();
Console.WriteLine("  CheckoutService did not change — the container controls what it gets.");
Pause();

Console.WriteLine("  Done.");
