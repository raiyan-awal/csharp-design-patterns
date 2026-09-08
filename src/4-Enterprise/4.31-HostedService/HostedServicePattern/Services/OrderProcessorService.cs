using HostedServicePattern.Core;
using HostedServicePattern.Infrastructure;

namespace HostedServicePattern.Services;

public sealed class OrderProcessorService : BackgroundService
{
    private readonly OrderChannel _channel;
    private readonly OrderStats _stats;
    private int _processed;

    public int ProcessedCount => _processed;

    public OrderProcessorService(OrderChannel channel, OrderStats stats)
        : base("order-processor")
    {
        _channel = channel;
        _stats   = stats;
    }

    // Uses CancellationToken.None so the reader always drains all queued orders
    // before returning, even when StopAsync has been called. The writer is
    // completed in StopAsync before the token is cancelled, which causes
    // ReadAllAsync to finish naturally once the queue is empty.
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var order in _channel.Reader.ReadAllAsync(CancellationToken.None))
        {
            _stats.Record(order);
            Interlocked.Increment(ref _processed);
        }
    }

    public override async Task StopAsync(CancellationToken ct = default)
    {
        _channel.Writer.TryComplete();  // seal the channel; ExecuteAsync drains and exits
        await base.StopAsync(ct);
    }
}
