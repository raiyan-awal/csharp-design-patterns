using HostedServicePattern.Core;
using HostedServicePattern.Domain;
using HostedServicePattern.Infrastructure;

namespace HostedServicePattern.Services;

public sealed class MetricsCollectorService : BackgroundService
{
    private readonly OrderStats _stats;
    private readonly TimeSpan _interval;
    private readonly Func<DateTimeOffset> _clock;
    private readonly List<MetricsSnapshot> _snapshots = [];
    private readonly Lock _lock = new();

    public MetricsCollectorService(OrderStats stats,
                                   TimeSpan? interval = null,
                                   Func<DateTimeOffset>? clock = null)
        : base("metrics-collector")
    {
        _stats    = stats;
        _interval = interval ?? TimeSpan.FromSeconds(30);
        _clock    = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public IReadOnlyList<MetricsSnapshot> Snapshots
    {
        get { lock (_lock) { return [.. _snapshots]; } }
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(_interval, ct); }
            catch (OperationCanceledException) { return; }
            var (count, total) = _stats.Read();
            lock (_lock) { _snapshots.Add(new MetricsSnapshot(_clock(), count, total)); }
        }
    }
}
