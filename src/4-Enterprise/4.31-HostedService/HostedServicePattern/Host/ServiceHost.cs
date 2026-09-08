using HostedServicePattern.Core;

namespace HostedServicePattern.Host;

public sealed class ServiceHost
{
    private readonly List<IHostedService> _services = [];

    public void Register(IHostedService service) => _services.Add(service);

    public IReadOnlyList<IHostedService> Services => _services.AsReadOnly();

    public async Task StartAsync(CancellationToken ct = default)
    {
        foreach (var svc in _services)
            await svc.StartAsync(ct);
    }

    // Stop in reverse registration order — dependencies started last are stopped first.
    public async Task StopAsync(CancellationToken ct = default)
    {
        foreach (var svc in Enumerable.Reverse(_services))
            await svc.StopAsync(ct);
    }
}
