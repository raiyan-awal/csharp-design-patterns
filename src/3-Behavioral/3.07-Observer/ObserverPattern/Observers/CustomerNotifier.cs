namespace ObserverPattern;

public sealed class CustomerNotifier : IOrderObserver
{
    private readonly List<string> _notifications = [];
    public IReadOnlyList<string> Notifications => _notifications;

    public void OnOrderUpdated(Order order, OrderStatus previousStatus)
    {
        var message = order.Status switch
        {
            OrderStatus.Processing => $"We're preparing your order #{order.OrderId}!",
            OrderStatus.Shipped    => $"Order #{order.OrderId} is on its way!",
            OrderStatus.Delivered  => $"Order #{order.OrderId} has been delivered. Enjoy!",
            OrderStatus.Cancelled  => $"Order #{order.OrderId} has been cancelled.",
            _                      => null
        };

        if (message is not null)
        {
            _notifications.Add(message);
            Console.WriteLine($"  [CustomerNotifier] Push → {message}");
        }
    }
}
