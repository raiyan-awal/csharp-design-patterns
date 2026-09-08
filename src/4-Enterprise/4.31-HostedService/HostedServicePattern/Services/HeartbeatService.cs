using HostedServicePattern.Core;

namespace HostedServicePattern.Services;

public sealed class HeartbeatService : BackgroundService
{
    private readonly TimeSpan _interval;
    private readonly Action<string> _log;
    private int _pulses;

    public HeartbeatService(TimeSpan? interval = null, Action<string>? log = null)
        : base("heartbeat")
    {
        _interval = interval ?? TimeSpan.FromSeconds(5);
        _log      = log ?? Console.WriteLine;
    }

    public int Pulses => _pulses;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(_interval, ct); }
            catch (OperationCanceledException) { return; }
            Interlocked.Increment(ref _pulses);
            _log($"  [{Name}] pulse #{_pulses}");
        }
    }
}
