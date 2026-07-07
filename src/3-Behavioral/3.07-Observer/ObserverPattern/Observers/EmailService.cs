namespace ObserverPattern;

public sealed class EmailService : IOrderObserver
{
    private readonly List<(OrderStatus Status, string Subject)> _sent = [];
    public IReadOnlyList<(OrderStatus Status, string Subject)> SentEmails => _sent;

    public void OnOrderUpdated(Order order, OrderStatus previousStatus)
    {
        var subject = order.Status switch
        {
            OrderStatus.Placed    => $"Order #{order.OrderId} confirmed — £{order.TotalAmount:F2}",
            OrderStatus.Shipped   => $"Your order #{order.OrderId} is on its way!",
            OrderStatus.Delivered => $"Order #{order.OrderId} delivered — thank you!",
            OrderStatus.Cancelled => $"Order #{order.OrderId} has been cancelled",
            _                     => null
        };

        if (subject is not null)
        {
            _sent.Add((order.Status, subject));
            Console.WriteLine($"  [EmailService] Sent: \"{subject}\"");
        }
    }
}
