namespace NullObjectPattern.Notifiers;

public sealed class SmsNotifier : ICustomerNotifier
{
    public void NotifyOrderPlaced(Order order)
        => Console.WriteLine($"  [SMS → {order.Phone}] Order #{order.OrderId} placed. Total: ${order.Total:F2}");

    public void NotifyOrderShipped(Order order, string trackingNumber)
        => Console.WriteLine($"  [SMS → {order.Phone}] Your order shipped! Track: {trackingNumber}");

    public void NotifyOrderCancelled(Order order, string reason)
        => Console.WriteLine($"  [SMS → {order.Phone}] Order #{order.OrderId} has been cancelled.");

    public void NotifyRefundIssued(Order order, decimal amount)
        => Console.WriteLine($"  [SMS → {order.Phone}] Refund of ${amount:F2} is on its way.");
}
