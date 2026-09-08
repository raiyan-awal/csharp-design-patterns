using HostedServicePattern.Host;
using HostedServicePattern.Infrastructure;
using HostedServicePattern.Services;

Console.WriteLine("=== 4.31 Hosted Service / Background Worker — Maple Commerce ===\n");

// ─── 1. HeartbeatService ──────────────────────────────────────────────────────
Console.WriteLine("─── 1. HeartbeatService — periodic liveness pulse ───");
Console.WriteLine("  Starting... (3 pulses at 300 ms each, then graceful stop)\n");

var heartbeat = new HeartbeatService(interval: TimeSpan.FromMilliseconds(300));
await heartbeat.StartAsync();
await Task.Delay(1000);   // ~3 pulses
await heartbeat.StopAsync();

Console.WriteLine($"\n  Stopped. Total pulses emitted: {heartbeat.Pulses}");

Pause();

// ─── 2. OrderProcessorService — channel drain on graceful shutdown ─────────────
Console.WriteLine("─── 2. OrderProcessorService — queue-based order processing ───");

var channel = new OrderChannel();
var stats   = new OrderStats();
var processor = new OrderProcessorService(channel, stats);

// Enqueue 6 Canadian orders before starting — demonstrates drain on stop
string[] customers = ["Mei Lin", "David Okafor", "Sophie Tremblay", "Amir Khalil", "Emma Lavoie", "Noah Park"];
string[] items     = ["Hockey Stick", "Parka", "Maple Syrup Gift Set", "Toque", "Canoe Paddle", "Fleece Vest"];
decimal[] prices   = [179.99m, 249.99m, 45.00m, 29.99m, 399.00m, 89.99m];

for (var i = 0; i < 6; i++)
    await channel.Writer.WriteAsync(new(
        Id: $"ORD-{i + 1:D3}",
        CustomerName: customers[i],
        Item: items[i],
        AmountCAD: prices[i]));

Console.WriteLine($"  Enqueued 6 orders. Starting processor...");
await processor.StartAsync();
await processor.StopAsync();   // TryComplete() + drain — all 6 must be processed

var (count, total) = stats.Read();
Console.WriteLine($"\n  Processed: {processor.ProcessedCount} orders");
Console.WriteLine($"  Total revenue: ${total:N2} CAD  (avg ${total / count:N2})");
Console.Write("  Orders: ");
Console.WriteLine(string.Join(", ", customers.Select((c, i) => $"{c} (${prices[i]:N2})")));

Pause();

// ─── 3. MetricsCollectorService — periodic snapshot of live stats ─────────────
Console.WriteLine("─── 3. MetricsCollectorService — periodic revenue snapshots ───");

var stats2    = new OrderStats();
var collector = new MetricsCollectorService(stats2, interval: TimeSpan.FromMilliseconds(250));

await collector.StartAsync();

// Simulate orders arriving over time
Console.WriteLine("  Enqueuing orders gradually...");
await Task.Delay(100);
stats2.Record(new("ORD-A", "Fatima Malik",    "Winter Boots",  149.99m));
await Task.Delay(300);   // snapshot #1 fires (~t=250ms)
stats2.Record(new("ORD-B", "James Beaumont",  "Down Jacket",   319.99m));
await Task.Delay(300);   // snapshot #2 fires (~t=500ms)
stats2.Record(new("ORD-C", "Priya Nair",      "Wool Blanket",   89.99m));
await Task.Delay(300);   // snapshot #3 fires (~t=750ms)

await collector.StopAsync();

Console.WriteLine($"\n  Snapshots taken: {collector.Snapshots.Count}");
foreach (var snap in collector.Snapshots)
    Console.WriteLine($"    {snap.TakenAt:HH:mm:ss.fff}  orders={snap.OrderCount,2}  " +
                      $"revenue=${snap.TotalRevenueCAD:N2} CAD");

Pause();

// ─── 4. Full pipeline via ServiceHost ────────────────────────────────────────
Console.WriteLine("─── 4. ServiceHost — three services, unified lifecycle ───");

var channel4   = new OrderChannel();
var stats4     = new OrderStats();
var host       = new ServiceHost();

host.Register(new HeartbeatService(interval: TimeSpan.FromMilliseconds(300)));
host.Register(new OrderProcessorService(channel4, stats4));
host.Register(new MetricsCollectorService(stats4, interval: TimeSpan.FromMilliseconds(300)));

Console.WriteLine($"  Starting {host.Services.Count} services...");
await host.StartAsync();

// Enqueue a few orders while everything runs
await channel4.Writer.WriteAsync(new("ORD-X1", "Lucas Girard",   "Snowshoes",    229.99m));
await channel4.Writer.WriteAsync(new("ORD-X2", "Amara Diallo",   "Ice Auger",    349.00m));
await channel4.Writer.WriteAsync(new("ORD-X3", "Chen Wei",       "Ski Goggles",   79.99m));

await Task.Delay(900);   // let all services run

Console.WriteLine($"\n  Stopping all services (reverse order)...");
await host.StopAsync();

var (c4, t4) = stats4.Read();
Console.WriteLine($"  All services stopped cleanly.");
Console.WriteLine($"  Orders processed: {c4}  |  Revenue: ${t4:N2} CAD");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
