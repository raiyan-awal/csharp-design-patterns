using NullObjectPattern;
using NullObjectPattern.Loggers;
using NullObjectPattern.Notifiers;

namespace NullObjectPattern.Tests;

public class NullObjectTests
{
    private static Order MakeOrder() =>
        new("ORD-001", "Test User", "test@example.ca", "+1-416-555-0100", 99.99m);

    // ── Null implementations do not throw ─────────────────────────────────────

    [Fact]
    public void NullCustomerNotifier_AllMethods_DoNotThrow()
    {
        var order = MakeOrder();
        var sut   = NullCustomerNotifier.Instance;

        sut.NotifyOrderPlaced(order);
        sut.NotifyOrderShipped(order, "TRK-001");
        sut.NotifyOrderCancelled(order, "Test reason");
        sut.NotifyRefundIssued(order, 50.00m);
        // Reaching here means no exception was thrown — the Null Object is safe
    }

    [Fact]
    public void NullAuditLogger_Log_DoesNotThrow()
    {
        var sut = NullAuditLogger.Instance;
        sut.Log("ORD-001", "TestAction");
        sut.Log("ORD-001", "TestAction", "with details");
    }

    // ── Null implementations are singletons ───────────────────────────────────

    [Fact]
    public void NullCustomerNotifier_IsSingleton()
    {
        Assert.Same(NullCustomerNotifier.Instance, NullCustomerNotifier.Instance);
    }

    [Fact]
    public void NullAuditLogger_IsSingleton()
    {
        Assert.Same(NullAuditLogger.Instance, NullAuditLogger.Instance);
    }

    // ── Initial order state ───────────────────────────────────────────────────

    [Fact]
    public void Order_InitialStatus_IsPending()
    {
        var order = MakeOrder();
        Assert.Equal("Pending", order.Status);
    }

    // ── OrderService updates status correctly regardless of implementation ─────

    [Fact]
    public void PlaceOrder_UpdatesStatusToConfirmed()
    {
        var order = MakeOrder();
        var svc   = new OrderService(NullCustomerNotifier.Instance, NullAuditLogger.Instance);

        svc.PlaceOrder(order);

        Assert.Equal("Confirmed", order.Status);
    }

    [Fact]
    public void ShipOrder_UpdatesStatusToShipped()
    {
        var order = MakeOrder();
        var svc   = new OrderService(NullCustomerNotifier.Instance, NullAuditLogger.Instance);

        svc.ShipOrder(order, "TRK-002");

        Assert.Equal("Shipped", order.Status);
    }

    [Fact]
    public void CancelOrder_UpdatesStatusToCancelled()
    {
        var order = MakeOrder();
        var svc   = new OrderService(NullCustomerNotifier.Instance, NullAuditLogger.Instance);

        svc.CancelOrder(order, "Customer request");

        Assert.Equal("Cancelled", order.Status);
    }

    // ── OrderService works with any combination of real/null dependencies ──────

    [Fact]
    public void OrderService_AllNullObjects_CompletesFullLifecycleWithoutError()
    {
        var order = MakeOrder();
        var svc   = new OrderService(NullCustomerNotifier.Instance, NullAuditLogger.Instance);

        svc.PlaceOrder(order);
        svc.ShipOrder(order, "TRK-003");
        svc.IssueRefund(order, 99.99m);

        Assert.Equal("Shipped", order.Status);
    }

    [Fact]
    public void OrderService_RealLogger_NullNotifier_DoesNotThrow()
    {
        var order = MakeOrder();
        var svc   = new OrderService(NullCustomerNotifier.Instance, new ConsoleAuditLogger());

        svc.PlaceOrder(order);
        svc.ShipOrder(order, "TRK-004");

        Assert.Equal("Shipped", order.Status);
    }

    [Fact]
    public void OrderService_NullLogger_RealNotifier_DoesNotThrow()
    {
        var order = MakeOrder();
        var svc   = new OrderService(new EmailNotifier(), NullAuditLogger.Instance);

        svc.PlaceOrder(order);
        svc.CancelOrder(order, "Test cancellation");

        Assert.Equal("Cancelled", order.Status);
    }
}
