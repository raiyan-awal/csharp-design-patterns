namespace HostedServicePattern.Core;

// Mirrors the pattern of Microsoft.Extensions.Hosting.BackgroundService.
// StartAsync fires ExecuteAsync on the thread pool and returns immediately,
// letting the host continue starting other services in parallel.
// StopAsync cancels the token passed to ExecuteAsync and waits for it to finish.
public abstract class BackgroundService : IHostedService
{
    private Task? _executeTask;
    private CancellationTokenSource? _cts;

    public string Name { get; }

    protected BackgroundService(string name) => Name = name;

    protected abstract Task ExecuteAsync(CancellationToken ct);

    public virtual Task StartAsync(CancellationToken ct = default)
    {
        _cts         = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _executeTask = ExecuteAsync(_cts.Token);
        // If ExecuteAsync returned synchronously (already done or cancelled),
        // surface any fault immediately; otherwise signal start complete and
        // let the task continue in the background.
        return _executeTask.IsCompleted ? _executeTask : Task.CompletedTask;
    }

    public virtual async Task StopAsync(CancellationToken ct = default)
    {
        if (_executeTask is null) return;
        try { _cts!.Cancel(); } catch (ObjectDisposedException) { }
        try { await _executeTask.ConfigureAwait(false); }
        catch (OperationCanceledException) { }  // expected on shutdown
    }
}
