using HostedServicePattern.Domain;

namespace HostedServicePattern.Infrastructure;

public sealed class OrderStats
{
    private int _count;
    private decimal _totalCAD;
    private readonly Lock _lock = new();

    public void Record(Order order)
    {
        lock (_lock) { _count++; _totalCAD += order.AmountCAD; }
    }

    public (int Count, decimal TotalCAD) Read()
    {
        lock (_lock) { return (_count, _totalCAD); }
    }
}
