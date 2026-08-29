using RetryPattern.Core;
using RetryPattern.Services;

Console.WriteLine("=== Maple Pay — Retry Pattern Demo ===\n");

var gateway = new SimulatedPaymentGateway();

static void PrintRetry(Exception ex, int attempt, TimeSpan delay) =>
    Console.WriteLine($"  [Attempt {attempt} failed: {ex.Message}]  Retrying in {delay.TotalMilliseconds:F0}ms...");

static void PrintResult(PaymentResult result) =>
    Console.WriteLine($"\n  ✓ Payment approved: {result.TransactionId} | ${result.AmountCAD:F2} CAD");

static void PrintExhausted(RetryExhaustedException ex) =>
    Console.WriteLine($"\n  ✗ [RETRIES EXHAUSTED] {ex.Message}\n  Root cause: {ex.InnerException!.Message}");

// ── Section 1: Fixed Delay ────────────────────────────────────────────────
Console.WriteLine("--- Fixed Delay Retry (2 transient failures, then success) ---");

gateway.FailTimes(2);

var fixedPolicy = new RetryPolicy(new RetryOptions
{
    MaxAttempts   = 5,
    DelayStrategy = DelayStrategy.Fixed(TimeSpan.FromMilliseconds(200)),
    ShouldRetry   = ex => ex is HttpRequestException,
    OnRetry       = PrintRetry
});

try
{
    var result = fixedPolicy.Execute(() =>
        gateway.ProcessPayment("tok_visa_4111", 149.99m, "ORD-1001"));
    PrintResult(result);
}
catch (RetryExhaustedException ex) { PrintExhausted(ex); }

Pause();

// ── Section 2: Exponential Back-off ───────────────────────────────────────
Console.WriteLine("--- Exponential Back-off (delays double with each attempt) ---");

gateway.FailTimes(3);

var exponentialPolicy = new RetryPolicy(new RetryOptions
{
    MaxAttempts   = 5,
    DelayStrategy = DelayStrategy.Exponential(TimeSpan.FromMilliseconds(100)),
    ShouldRetry   = ex => ex is HttpRequestException,
    OnRetry       = PrintRetry
});

try
{
    var result = exponentialPolicy.Execute(() =>
        gateway.ProcessPayment("tok_mc_5500", 89.00m, "ORD-1002"));
    PrintResult(result);
}
catch (RetryExhaustedException ex) { PrintExhausted(ex); }

Pause();

// ── Section 3: Non-Retryable Exception ────────────────────────────────────
Console.WriteLine("--- Non-Retryable Exception (card declined — retried zero times) ---");

gateway.Decline();
var callsBefore = gateway.CallCount;

var strictPolicy = new RetryPolicy(new RetryOptions
{
    MaxAttempts   = 5,
    DelayStrategy = DelayStrategy.Fixed(TimeSpan.FromMilliseconds(200)),
    ShouldRetry   = ex => ex is HttpRequestException,   // PaymentDeclinedException is excluded
    OnRetry       = PrintRetry
});

try
{
    var result = strictPolicy.Execute(() =>
        gateway.ProcessPayment("tok_declined_9999", 250.00m, "ORD-1003"));
    PrintResult(result);
}
catch (RetryExhaustedException ex) { PrintExhausted(ex); }
catch (PaymentDeclinedException ex)
{
    Console.WriteLine($"  ✗ {ex.Message}");
    Console.WriteLine($"  Gateway calls before: {callsBefore} | after: {gateway.CallCount}");
    Console.WriteLine($"  Calls attempted: {gateway.CallCount - callsBefore} (no retries — failed immediately)");
}

Pause();

// ── Section 4: Retries Exhausted ──────────────────────────────────────────
Console.WriteLine("--- Retries Exhausted (all 3 attempts fail) ---");

gateway.SetHealthy();
gateway.FailTimes(10);

var exhaustPolicy = new RetryPolicy(new RetryOptions
{
    MaxAttempts   = 3,
    DelayStrategy = DelayStrategy.Fixed(TimeSpan.FromMilliseconds(100)),
    ShouldRetry   = ex => ex is HttpRequestException,
    OnRetry       = PrintRetry
});

try
{
    var result = exhaustPolicy.Execute(() =>
        gateway.ProcessPayment("tok_visa_4111", 75.00m, "ORD-1004"));
    PrintResult(result);
}
catch (RetryExhaustedException ex) { PrintExhausted(ex); }

Console.WriteLine("\n=== Demo complete ===");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
