namespace NullObjectPattern;

// Context that uses the optional dependencies.
// Both constructor parameters are non-nullable — callers pass a Null implementation
// when the behaviour is not wanted. This class contains zero null checks.
public sealed class OrderService
{
    private readonly ICustomerNotifier _notifier;
    private readonly IAuditLogger      _logger;

    public OrderService(ICustomerNotifier notifier, IAuditLogger logger)
    {
        _notifier = notifier;
        _logger   = logger;
    }

    public void PlaceOrder(Order order)
    {
        order.UpdateStatus("Confirmed");
        _logger.Log(order.OrderId, "PlaceOrder", $"${order.Total:F2} — {order.CustomerName}");
        _notifier.NotifyOrderPlaced(order);
    }

    public void ShipOrder(Order order, string trackingNumber)
    {
        order.UpdateStatus("Shipped");
        _logger.Log(order.OrderId, "ShipOrder", $"Tracking: {trackingNumber}");
        _notifier.NotifyOrderShipped(order, trackingNumber);
    }

    public void CancelOrder(Order order, string reason)
    {
        order.UpdateStatus("Cancelled");
        _logger.Log(order.OrderId, "CancelOrder", reason);
        _notifier.NotifyOrderCancelled(order, reason);
    }

    public void IssueRefund(Order order, decimal amount)
    {
        _logger.Log(order.OrderId, "IssueRefund", $"${amount:F2}");
        _notifier.NotifyRefundIssued(order, amount);
    }
}
