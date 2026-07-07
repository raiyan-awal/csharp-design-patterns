namespace ObserverPattern;

// The Subject (Observable). Maintains a list of observers and notifies them on state change.
public sealed class Order : IOrderSubject
{
    private readonly List<IOrderObserver> _observers = [];
    private OrderStatus _status = OrderStatus.Placed;

    public string      OrderId     { get; }
    public string      CustomerId  { get; }
    public decimal     TotalAmount { get; }
    public OrderStatus Status      => _status;

    public Order(string orderId, string customerId, decimal totalAmount)
    {
        OrderId     = orderId;
        CustomerId  = customerId;
        TotalAmount = totalAmount;
    }

    // ── Subject interface ─────────────────────────────────────────────────────

    public void Subscribe(IOrderObserver observer)
    {
        if (!_observers.Contains(observer))
            _observers.Add(observer);
    }

    public void Unsubscribe(IOrderObserver observer) => _observers.Remove(observer);

    // ── State change ──────────────────────────────────────────────────────────

    public void UpdateStatus(OrderStatus newStatus)
    {
        if (newStatus == _status) return;

        var previous = _status;
        _status = newStatus;

        Console.WriteLine($"\n  [Order #{OrderId}] {previous} → {newStatus}");
        NotifyAll(previous);
    }

    private void NotifyAll(OrderStatus previous)
    {
        // ToList() snapshot prevents issues if an observer unsubscribes during notification
        foreach (var observer in _observers.ToList())
            observer.OnOrderUpdated(this, previous);
    }
}
