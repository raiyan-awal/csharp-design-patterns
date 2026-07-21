namespace NullObjectPattern.Notifiers;

// Null Object — implements ICustomerNotifier with safe no-ops.
// Callers inject this when notifications are not needed (batch jobs, tests, dry-run
// mode). OrderService calls its methods unconditionally; no null checks required.
// Singleton: no state, no benefit to creating multiple instances.
public sealed class NullCustomerNotifier : ICustomerNotifier
{
    public static readonly NullCustomerNotifier Instance = new();
    private NullCustomerNotifier() { }

    public void NotifyOrderPlaced(Order order)                         { }
    public void NotifyOrderShipped(Order order, string trackingNumber) { }
    public void NotifyOrderCancelled(Order order, string reason)       { }
    public void NotifyRefundIssued(Order order, decimal amount)        { }
}
