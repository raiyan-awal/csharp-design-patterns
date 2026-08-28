using CircuitBreakerPattern.Core;
using CircuitBreakerPattern.Services;

Console.WriteLine("=== Maple Commerce — Circuit Breaker Demo ===\n");

var shippingService = new SimulatedShippingRateService();
var cb = new CircuitBreaker(new CircuitBreakerOptions
{
    FailureThreshold = 3,
    SuccessThreshold = 2,
    ResetTimeout     = TimeSpan.FromSeconds(3)
});

static void PrintState(CircuitBreaker cb) =>
    Console.WriteLine($"  [Circuit: {cb.State,-8} | Failures: {cb.FailureCount} | Successes: {cb.SuccessCount}]");

static void TryGetRate(CircuitBreaker cb, IShippingRateService svc,
                       string origin, string destination, decimal weight)
{
    try
    {
        var rate = cb.Execute(() => svc.GetRate(origin, destination, weight));
        Console.WriteLine($"  ✓ {origin} → {destination} | ${rate.PriceCAD:F2} CAD | {rate.EstimatedDays} days");
    }
    catch (CircuitBreakerOpenException ex)
    {
        Console.WriteLine($"  ✗ [CIRCUIT OPEN] {ex.Message}");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"  ✗ [SERVICE ERROR] {ex.Message}");
    }
}

// ── Section 1: Normal Operation ───────────────────────────────────────────
Console.WriteLine("--- Normal Operation (Circuit: Closed) ---");

TryGetRate(cb, shippingService, "Toronto, ON",  "Vancouver, BC",   1.2m);
TryGetRate(cb, shippingService, "Montreal, QC", "Calgary, AB",     0.8m);
TryGetRate(cb, shippingService, "Ottawa, ON",   "Halifax, NS",     2.0m);
PrintState(cb);

Pause();

// ── Section 2: Service Degradation ────────────────────────────────────────
Console.WriteLine("--- Service Degradation (Canada Post API failing) ---");

shippingService.SetFailing();
Console.WriteLine("  [Canada Post API set to FAILING]\n");

TryGetRate(cb, shippingService, "Toronto, ON",  "Vancouver, BC",  1.0m);  // failure 1
PrintState(cb);
TryGetRate(cb, shippingService, "Montreal, QC", "Calgary, AB",    0.5m);  // failure 2
PrintState(cb);
TryGetRate(cb, shippingService, "Ottawa, ON",   "Halifax, NS",    1.5m);  // failure 3 → OPEN
PrintState(cb);

Pause();

// ── Section 3: Open Circuit ───────────────────────────────────────────────
Console.WriteLine("--- Open Circuit (rejecting immediately, service not called) ---");

const int attempts = 2;
var callsBefore = shippingService.CallCount;
TryGetRate(cb, shippingService, "Winnipeg, MB", "Victoria, BC",   3.0m);  // rejected
TryGetRate(cb, shippingService, "Edmonton, AB", "Toronto, ON",    2.5m);  // rejected
var callsAfter = shippingService.CallCount;

Console.WriteLine($"\n  Attempted calls: {attempts} | Reached service: {callsAfter - callsBefore}");
Console.WriteLine($"  Calls blocked by circuit breaker: {attempts - (callsAfter - callsBefore)}");
PrintState(cb);

Pause();

// ── Section 4: Half-Open — Failure Re-Opens ───────────────────────────────
Console.WriteLine("--- Half-Open: Failure Re-Opens Circuit ---");
Console.WriteLine("  Service is still failing. Waiting 3s for reset timeout to expire...");
Thread.Sleep(3100);

Console.WriteLine("  [Timeout elapsed — next call probes the service (Half-Open)]\n");

// First call transitions Open → Half-Open, then the action runs — and fails
TryGetRate(cb, shippingService, "Toronto, ON", "Vancouver, BC",  1.0m);  // Half-Open probe → fail → OPEN
PrintState(cb);

Console.WriteLine("\n  A single failure in Half-Open immediately re-opens the circuit.");
Console.WriteLine("  The service gets no partial credit — it must prove it has recovered.");

Pause();

// ── Section 5: Recovery (Half-Open → Closed) ─────────────────────────────
Console.WriteLine("--- Recovery (waiting for reset timeout, then Half-Open → Closed) ---");
Console.WriteLine($"  Waiting 3s for reset timeout to expire...");
Thread.Sleep(3100);

shippingService.SetHealthy();
Console.WriteLine("  [Canada Post API restored]\n");

TryGetRate(cb, shippingService, "Toronto, ON",  "Vancouver, BC",  1.0m);  // success 1 → HalfOpen
PrintState(cb);
TryGetRate(cb, shippingService, "Montreal, QC", "Calgary, AB",    0.5m);  // success 2 → Closed
PrintState(cb);

Console.WriteLine("\n  Circuit recovered — normal operation resumed:");
TryGetRate(cb, shippingService, "Ottawa, ON",   "Halifax, NS",    2.0m);
PrintState(cb);

Console.WriteLine("\n=== Demo complete ===");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
