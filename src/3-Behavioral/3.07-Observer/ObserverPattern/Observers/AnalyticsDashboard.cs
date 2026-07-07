namespace ObserverPattern;

public sealed class AnalyticsDashboard : IOrderObserver
{
    private readonly List<(OrderStatus From, OrderStatus To, DateTime At)> _transitions = [];
    public IReadOnlyList<(OrderStatus From, OrderStatus To, DateTime At)> Transitions => _transitions;

    public void OnOrderUpdated(Order order, OrderStatus previousStatus)
    {
        _transitions.Add((previousStatus, order.Status, DateTime.UtcNow));
        Console.WriteLine($"  [Analytics] Recorded: {previousStatus} → {order.Status}");
    }
}
