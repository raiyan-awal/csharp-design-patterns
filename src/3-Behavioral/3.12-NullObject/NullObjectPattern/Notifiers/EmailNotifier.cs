namespace NullObjectPattern.Notifiers;

public sealed class EmailNotifier : ICustomerNotifier
{
    public void NotifyOrderPlaced(Order order)
        => Console.WriteLine($"  [EMAIL → {order.Email}] Order #{order.OrderId} confirmed — total ${order.Total:F2}");

    public void NotifyOrderShipped(Order order, string trackingNumber)
        => Console.WriteLine($"  [EMAIL → {order.Email}] Order #{order.OrderId} shipped — tracking: {trackingNumber}");

    public void NotifyOrderCancelled(Order order, string reason)
        => Console.WriteLine($"  [EMAIL → {order.Email}] Order #{order.OrderId} cancelled — reason: {reason}");

    public void NotifyRefundIssued(Order order, decimal amount)
        => Console.WriteLine($"  [EMAIL → {order.Email}] Refund of ${amount:F2} issued for order #{order.OrderId}");
}
