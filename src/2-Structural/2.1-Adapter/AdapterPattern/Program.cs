using AdapterPattern;
using AdapterPattern.ThirdParty;

// ============================================================
// ADAPTER PATTERN — DEMO
// ============================================================
// Shows how three completely incompatible payment SDKs (Stripe,
// PayPal, Square) are made interchangeable through adapters.
// OrderService only ever sees IPaymentProcessor.
// ============================================================

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine("\n");
}

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║           ADAPTER PATTERN — Payment Gateways         ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("The Problem: Your app depends on IPaymentProcessor.");
Console.WriteLine("Three SDKs exist — each with a completely different API.");
Console.WriteLine("Solution: wrap each SDK in an Adapter that speaks your language.");

Pause();

// ── DEMO 1: Stripe ────────────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 1 — Charging and refunding via Stripe");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

// Wire up: Stripe SDK → StripeAdapter → IPaymentProcessor
IPaymentProcessor stripeProcessor = new StripeAdapter(new StripeGateway("sk_test_abc123"));
var stripeOrderService = new OrderService(stripeProcessor);

string stripeTxId = stripeOrderService.PlaceOrder("ORD-001", "tok_visa", 4999, "USD");
stripeOrderService.CancelOrder("ORD-001", stripeTxId, 4999);

Pause();

// ── DEMO 2: PayPal ────────────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 2 — Charging and refunding via PayPal");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("OrderService code is identical — only the injected adapter changes.");
Console.WriteLine();

IPaymentProcessor paypalProcessor = new PayPalAdapter(new PayPalSdk("client_id", "client_secret"));
var paypalOrderService = new OrderService(paypalProcessor);

string paypalTxId = paypalOrderService.PlaceOrder("ORD-002", "PP_CARD_TOKEN_XYZ", 12500, "USD");
paypalOrderService.CancelOrder("ORD-002", paypalTxId, 12500);

Pause();

// ── DEMO 3: Square ────────────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 3 — Charging and refunding via Square");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

IPaymentProcessor squareProcessor = new SquareAdapter(new SquareApi("sq0atp-access_token"));
var squareOrderService = new OrderService(squareProcessor);

string squareTxId = squareOrderService.PlaceOrder("ORD-003", "cnon:card-nonce-ok", 7500, "USD");
squareOrderService.CancelOrder("ORD-003", squareTxId, 7500);

Pause();

// ── DEMO 4: Runtime gateway swap ─────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 4 — Swapping gateways at runtime (A/B test scenario)");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("Imagine routing 50% of traffic to Stripe, 50% to PayPal.");
Console.WriteLine("OrderService is reused as-is — only the adapter injected changes.");
Console.WriteLine();

IPaymentProcessor[] gateways =
[
    new StripeAdapter(new StripeGateway("sk_live_key")),
    new PayPalAdapter(new PayPalSdk("live_client", "live_secret")),
    new SquareAdapter(new SquareApi("sq0atp-live_token")),
];

string[] orders    = ["ORD-101", "ORD-102", "ORD-103"];
string[] cardTokens = ["tok_mastercard", "PP_CARD_789", "cnon:card-nonce-2"];
int[]    amounts   = [9900, 4500, 15000];

for (int i = 0; i < orders.Length; i++)
{
    // Pick gateway round-robin (simulates routing logic)
    IPaymentProcessor gateway = gateways[i % gateways.Length];
    var service = new OrderService(gateway);

    Console.WriteLine($"  Routing {orders[i]} → {gateway.ProcessorName}");
    service.PlaceOrder(orders[i], cardTokens[i], amounts[i], "USD");
    Console.WriteLine();
}

Pause();

// ── DEMO 5: Failed charge (bad card token) ────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 5 — Error handling: invalid card token");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("Each adapter translates gateway-specific errors into the");
Console.WriteLine("same PaymentResult.Fail(...) shape — uniform error handling.");
Console.WriteLine();

IPaymentProcessor[] allProcessors =
[
    new StripeAdapter(new StripeGateway("sk_test_key")),
    new PayPalAdapter(new PayPalSdk("client", "secret")),
    new SquareAdapter(new SquareApi("token")),
];

foreach (var processor in allProcessors)
{
    // Empty token — each SDK rejects it differently, but the adapter
    // normalises the failure into PaymentResult.Fail consistently.
    PaymentResult result = processor.Charge(string.Empty, 1000, "USD");
    Console.WriteLine($"  [{processor.ProcessorName}] {result}");
}

Console.WriteLine();
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("Summary:");
Console.WriteLine("  • OrderService has ZERO knowledge of Stripe, PayPal, or Square");
Console.WriteLine("  • Each adapter wraps one incompatible SDK — translation only");
Console.WriteLine("  • Adding a new gateway = add one new Adapter class");
Console.WriteLine("  • All existing code remains unchanged");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
