using ObserverPattern;

public class ObserverPatternTests
{
    private static Order MakeOrder(string id = "ORD-001", decimal amount = 99.99m)
        => new(id, "CUST-001", amount);

    // ── Subscribe / Unsubscribe ───────────────────────────────────────────────

    [Fact]
    public void Subscribe_ObserverReceivesNotificationOnStatusChange()
    {
        var order     = MakeOrder();
        var analytics = new AnalyticsDashboard();
        order.Subscribe(analytics);

        order.UpdateStatus(OrderStatus.Processing);

        Assert.Single(analytics.Transitions);
    }

    [Fact]
    public void Unsubscribe_ObserverNoLongerReceivesNotifications()
    {
        var order     = MakeOrder();
        var analytics = new AnalyticsDashboard();
        order.Subscribe(analytics);
        order.UpdateStatus(OrderStatus.Processing);

        order.Unsubscribe(analytics);
        order.UpdateStatus(OrderStatus.Shipped);

        Assert.Single(analytics.Transitions); // only the first one
    }

    [Fact]
    public void Subscribe_SameObserverTwice_OnlyNotifiedOnce()
    {
        var order     = MakeOrder();
        var analytics = new AnalyticsDashboard();
        order.Subscribe(analytics);
        order.Subscribe(analytics); // duplicate

        order.UpdateStatus(OrderStatus.Processing);

        Assert.Single(analytics.Transitions);
    }

    // ── Status change behaviour ───────────────────────────────────────────────

    [Fact]
    public void UpdateStatus_SameStatus_DoesNotNotifyObservers()
    {
        var order     = MakeOrder();
        var analytics = new AnalyticsDashboard();
        order.Subscribe(analytics);

        order.UpdateStatus(OrderStatus.Placed); // same as initial — no-op

        Assert.Empty(analytics.Transitions);
    }

    [Fact]
    public void UpdateStatus_PassesCorrectPreviousStatus()
    {
        var order     = MakeOrder();
        var analytics = new AnalyticsDashboard();
        order.Subscribe(analytics);

        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);

        Assert.Equal(OrderStatus.Placed,     analytics.Transitions[0].From);
        Assert.Equal(OrderStatus.Processing, analytics.Transitions[0].To);
        Assert.Equal(OrderStatus.Processing, analytics.Transitions[1].From);
        Assert.Equal(OrderStatus.Shipped,    analytics.Transitions[1].To);
    }

    [Fact]
    public void MultipleObservers_AllNotifiedOnStatusChange()
    {
        var order     = MakeOrder();
        var customer  = new CustomerNotifier();
        var warehouse = new WarehouseSystem();
        var analytics = new AnalyticsDashboard();
        order.Subscribe(customer);
        order.Subscribe(warehouse);
        order.Subscribe(analytics);

        order.UpdateStatus(OrderStatus.Processing);

        Assert.Single(customer.Notifications);
        Assert.Equal(1, warehouse.NotificationCount);
        Assert.Single(analytics.Transitions);
    }

    [Fact]
    public void LateSubscriber_OnlyReceivesEventsAfterSubscription()
    {
        var order  = MakeOrder();
        var early  = new AnalyticsDashboard();
        var late   = new AnalyticsDashboard();
        order.Subscribe(early);

        order.UpdateStatus(OrderStatus.Processing); // early only
        order.Subscribe(late);
        order.UpdateStatus(OrderStatus.Shipped);    // both

        Assert.Equal(2, early.Transitions.Count);
        Assert.Single(late.Transitions);
    }

    // ── CustomerNotifier ─────────────────────────────────────────────────────

    [Fact]
    public void CustomerNotifier_SendsNotificationOnShipped()
    {
        var order    = MakeOrder();
        var customer = new CustomerNotifier();
        order.Subscribe(customer);

        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);

        Assert.Equal(2, customer.Notifications.Count);
        Assert.Contains("on its way", customer.Notifications[1]);
    }

    [Fact]
    public void CustomerNotifier_DoesNotSendOnProcessingToShipped_WhenNothingMatches()
    {
        // Placed status change fires no notification (no case for Placed in CustomerNotifier)
        var order    = MakeOrder();
        var customer = new CustomerNotifier();
        order.Subscribe(customer);

        order.UpdateStatus(OrderStatus.Placed); // same status — no-op
        Assert.Empty(customer.Notifications);
    }

    // ── WarehouseSystem ──────────────────────────────────────────────────────

    [Fact]
    public void WarehouseSystem_StartsPickingOnProcessing()
    {
        var order     = MakeOrder();
        var warehouse = new WarehouseSystem();
        order.Subscribe(warehouse);

        order.UpdateStatus(OrderStatus.Processing);

        Assert.True(warehouse.IsPickingOrder);
    }

    [Fact]
    public void WarehouseSystem_UpdatesInventoryOnDelivered()
    {
        var order     = MakeOrder();
        var warehouse = new WarehouseSystem();
        order.Subscribe(warehouse);

        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);
        order.UpdateStatus(OrderStatus.Delivered);

        Assert.True(warehouse.IsInventoryUpdated);
    }

    [Fact]
    public void WarehouseSystem_StopsPickingOnCancelled()
    {
        var order     = MakeOrder();
        var warehouse = new WarehouseSystem();
        order.Subscribe(warehouse);

        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Cancelled);

        Assert.False(warehouse.IsPickingOrder);
    }

    // ── EmailService ─────────────────────────────────────────────────────────

    [Fact]
    public void EmailService_SendsEmailOnDelivered()
    {
        var order = MakeOrder();
        var email = new EmailService();
        order.Subscribe(email);

        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);
        order.UpdateStatus(OrderStatus.Delivered);

        Assert.Equal(2, email.SentEmails.Count); // Shipped + Delivered
        Assert.Equal(OrderStatus.Delivered, email.SentEmails[^1].Status);
    }

    // ── InvoiceService ───────────────────────────────────────────────────────

    [Fact]
    public void InvoiceService_GeneratesInvoiceOnDelivered()
    {
        var order   = MakeOrder(amount: 199.99m);
        var invoice = new InvoiceService();
        order.Subscribe(invoice);

        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);
        order.UpdateStatus(OrderStatus.Delivered);

        Assert.True(invoice.InvoiceGenerated);
        Assert.Equal(199.99m, invoice.InvoiceAmount);
    }

    [Fact]
    public void InvoiceService_DoesNotGenerateInvoiceBeforeDelivery()
    {
        var order   = MakeOrder();
        var invoice = new InvoiceService();
        order.Subscribe(invoice);

        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);

        Assert.False(invoice.InvoiceGenerated);
    }

    // ── AnalyticsDashboard ───────────────────────────────────────────────────

    [Fact]
    public void AnalyticsDashboard_RecordsEveryTransition()
    {
        var order     = MakeOrder();
        var analytics = new AnalyticsDashboard();
        order.Subscribe(analytics);

        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);
        order.UpdateStatus(OrderStatus.Delivered);

        Assert.Equal(3, analytics.Transitions.Count);
    }
}
