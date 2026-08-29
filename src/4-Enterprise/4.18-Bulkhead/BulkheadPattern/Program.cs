using BulkheadPattern.Core;
using BulkheadPattern.Services;

Console.WriteLine("=== Maple Connect — Bulkhead Pattern Demo ===\n");

var accountSvc = new SimulatedAccountService();
var networkSvc = new SimulatedNetworkService();

// ── Section 1: Normal Operation ───────────────────────────────────────────
Console.WriteLine("--- Normal Operation (3 concurrent calls, MaxConcurrency: 3) ---");

accountSvc.SetHealthy();
var normalBulkhead = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 3, MaxQueueSize = 0 });

var normalTasks = Enumerable.Range(1, 3).Select(i => Task.Run(() =>
{
    var result = normalBulkhead.Execute(() => accountSvc.GetAccount($"ACC-100{i}"));
    Console.WriteLine($"  ✓ [{i}] {result.AccountId} | {result.CustomerName} | {result.Plan}");
})).ToArray();

Task.WaitAll(normalTasks);
Console.WriteLine($"  [Available slots: {normalBulkhead.Available}/3]");

Pause();

// ── Section 2: Saturated — No Queue ──────────────────────────────────────
Console.WriteLine("--- Saturated (MaxConcurrency: 2, MaxQueueSize: 0 — excess calls rejected) ---");

accountSvc.SetLatency(TimeSpan.FromMilliseconds(400));
var saturatedBulkhead = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 2, MaxQueueSize = 0 });

var holding2 = new CountdownEvent(2);
var release2 = new ManualResetEventSlim(false);

// Two tasks hold both slots
var holders2 = Enumerable.Range(1, 2).Select(i => Task.Run(() =>
    saturatedBulkhead.Execute(() =>
    {
        holding2.Signal();
        release2.Wait();
        return accountSvc.GetAccount($"ACC-200{i}");
    }))).ToArray();

holding2.Wait();  // both slots are now occupied
Console.WriteLine("  [2 calls holding both execution slots...]\n");

// Two additional calls should be rejected immediately
var rejectTasks = Enumerable.Range(3, 2).Select(i => Task.Run(() =>
{
    try
    {
        saturatedBulkhead.Execute(() => accountSvc.GetAccount($"ACC-200{i}"));
    }
    catch (BulkheadRejectedException ex)
    {
        Console.WriteLine($"  ✗ [{i}] {ex.Message}");
    }
})).ToArray();

Task.WaitAll(rejectTasks);
release2.Set();
Task.WaitAll(holders2);
Console.WriteLine($"\n  [Available slots after release: {saturatedBulkhead.Available}/2]");

Pause();

// ── Section 3: Queue Absorbs Excess ──────────────────────────────────────
Console.WriteLine("--- Queue (MaxConcurrency: 2, MaxQueueSize: 2 — excess calls wait) ---");

accountSvc.SetLatency(TimeSpan.FromMilliseconds(300));
var queuedBulkhead = new BulkheadPolicy(new BulkheadOptions
{
    MaxConcurrency = 2,
    MaxQueueSize   = 2,
    QueueTimeout   = TimeSpan.FromSeconds(5)
});

var results3   = new string[4];
var gate3      = new CountdownEvent(4);

var queueTasks = Enumerable.Range(0, 4).Select(i => Task.Run(() =>
{
    gate3.Signal();
    gate3.Wait();  // all 4 tasks start simultaneously
    try
    {
        var result = queuedBulkhead.Execute(() => accountSvc.GetAccount($"ACC-300{i + 1}"));
        results3[i] = $"  ✓ [{i + 1}] {result.AccountId} — succeeded";
    }
    catch (BulkheadRejectedException ex)
    {
        results3[i] = $"  ✗ [{i + 1}] {ex.Message}";
    }
})).ToArray();

Task.WaitAll(queueTasks);
foreach (var r in results3) Console.WriteLine(r);
Console.WriteLine("  (calls 3 and 4 queued and waited for slots to free up)");

Pause();

// ── Section 4: Isolation ─────────────────────────────────────────────────
Console.WriteLine("--- Isolation (Account bulkhead saturated — Network bulkhead unaffected) ---");

accountSvc.SetLatency(TimeSpan.FromMilliseconds(400));
networkSvc.SetHealthy();

var accountBulkhead = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 2, MaxQueueSize = 0 });
var networkBulkhead = new BulkheadPolicy(new BulkheadOptions { MaxConcurrency = 2, MaxQueueSize = 0 });

var holding4 = new CountdownEvent(2);
var release4 = new ManualResetEventSlim(false);

// Saturate the account bulkhead
var accountHolders = Enumerable.Range(1, 2).Select(i => Task.Run(() =>
    accountBulkhead.Execute(() =>
    {
        holding4.Signal();
        release4.Wait();
        return accountSvc.GetAccount($"ACC-400{i}");
    }))).ToArray();

holding4.Wait();
Console.WriteLine("  [Account Service bulkhead: both slots occupied]\n");

// Account call rejected — bulkhead full
try
{
    accountBulkhead.Execute(() => accountSvc.GetAccount("ACC-4003"));
}
catch (BulkheadRejectedException ex)
{
    Console.WriteLine($"  ✗ Account Service: {ex.Message}");
}

// Network call succeeds — completely separate bulkhead
var networkResult = networkBulkhead.Execute(() => networkSvc.GetStatus("Ontario"));
Console.WriteLine($"  ✓ Network Service: {networkResult.Region} | {networkResult.Status} | {networkResult.TowersOnline}/{networkResult.TowersTotal} towers");
Console.WriteLine("\n  Account service is isolated — the slow Account Service did not affect Network Service.");

release4.Set();
Task.WaitAll(accountHolders);

Console.WriteLine("\n=== Demo complete ===");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
