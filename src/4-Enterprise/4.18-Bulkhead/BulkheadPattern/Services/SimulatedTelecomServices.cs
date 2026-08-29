namespace BulkheadPattern.Services;

public sealed class SimulatedAccountService
{
    private TimeSpan _latency;

    public void SetLatency(TimeSpan latency) => _latency = latency;
    public void SetHealthy()                 => _latency = TimeSpan.Zero;

    public AccountInfo GetAccount(string accountId)
    {
        if (_latency > TimeSpan.Zero) Thread.Sleep(_latency);
        return new AccountInfo(accountId, "Sarah Chen", "Unlimited Plus", 45.99m);
    }
}

public sealed class SimulatedNetworkService
{
    private TimeSpan _latency;

    public void SetLatency(TimeSpan latency) => _latency = latency;
    public void SetHealthy()                 => _latency = TimeSpan.Zero;

    public NetworkStatus GetStatus(string region)
    {
        if (_latency > TimeSpan.Zero) Thread.Sleep(_latency);
        return new NetworkStatus(region, "Operational", 142, 145);
    }
}
