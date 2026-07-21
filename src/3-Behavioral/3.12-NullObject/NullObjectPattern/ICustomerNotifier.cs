namespace NullObjectPattern;

// Abstraction that all notifier implementations — real and null — must satisfy.
// OrderService depends only on this interface; it never knows which implementation
// it holds, so swapping in the Null version requires no changes to the service.
public interface ICustomerNotifier
{
    void NotifyOrderPlaced(Order order);
    void NotifyOrderShipped(Order order, string trackingNumber);
    void NotifyOrderCancelled(Order order, string reason);
    void NotifyRefundIssued(Order order, decimal amount);
}
