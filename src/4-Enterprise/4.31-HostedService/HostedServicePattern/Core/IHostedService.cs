namespace HostedServicePattern.Core;

public interface IHostedService
{
    string Name { get; }
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}
