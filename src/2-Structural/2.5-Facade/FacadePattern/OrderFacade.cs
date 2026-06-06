namespace FacadePattern;

// ============================================================
// FACADE
// ============================================================
// OrderFacade is the single entry point for all order-related
// operations. It coordinates five subsystems — inventory,
// payment, shipping, notifications, audit — so clients never
// have to. The client calls one method; the Facade handles
// sequencing, compensation on failure, and logging.
//
// Key guarantee: if any step fails, previously completed steps
// are rolled back (e.g. reserved stock is released if payment
// is declined). Without the Facade every caller would have to
// implement this compensation logic themselves.

public sealed class OrderFacade
{
    private readonly InventoryService   _inventory;
    private readonly PaymentService     _payment;
    private readonly ShippingService    _shipping;
    private readonly NotificationService _notifications;
    private readonly AuditLogger        _audit;

    public OrderFacade(
        InventoryService    inventory,
        PaymentService      payment,
        ShippingService     shipping,
        NotificationService notifications,
        AuditLogger         audit)
    {
        _inventory     = inventory;
        _payment       = payment;
        _shipping      = shipping;
        _notifications = notifications;
        _audit         = audit;
    }

    // Convenience factory — creates a ready-to-use facade with default subsystems.
    public static OrderFacade CreateDefault(Dictionary<string, int>? stock = null)
        => new(
            new InventoryService(stock),
            new PaymentService(),
            new ShippingService(),
            new NotificationService(),
            new AuditLogger());

    // ── PlaceOrder ────────────────────────────────────────────
    // Orchestrates: stock check → reserve → charge → ship → notify → audit
    // On failure: compensates all steps already completed.
    public OrderResult PlaceOrder(Order order)
    {
        _audit.Log("PlaceOrder", $"Started — order {order.OrderId}");

        // 1. Verify stock for every line before touching anything
        foreach (var line in order.Lines)
        {
            if (!_inventory.IsAvailable(line.ProductId, line.Quantity))
            {
                _audit.Log("PlaceOrder", $"Aborted — '{line.ProductName}' out of stock");
                return OrderResult.Fail(order.OrderId, $"'{line.ProductName}' is out of stock.");
            }
        }

        // 2. Reserve stock
        foreach (var line in order.Lines)
            _inventory.Reserve(line.ProductId, line.Quantity);

        // 3. Charge payment
        decimal total   = order.Lines.Sum(l => l.LineTotal);
        var     payment = _payment.Charge(order.CardToken, total);

        if (!payment.Success)
        {
            // Compensate: release the reserved stock so it becomes available again
            foreach (var line in order.Lines)
                _inventory.Release(line.ProductId, line.Quantity);

            _audit.Log("PlaceOrder", $"Aborted — payment declined: {payment.Message}");
            return OrderResult.Fail(order.OrderId, $"Payment failed: {payment.Message}");
        }

        // 4. Create shipment
        string tracking = _shipping.CreateShipment(order.ShippingAddress, order.Lines);

        // 5. Notify customer
        _notifications.SendOrderConfirmation(order.CustomerEmail, order.OrderId, tracking);

        _audit.Log("PlaceOrder", $"Completed — tracking {tracking}, txn {payment.TransactionId}");
        return OrderResult.Ok(order.OrderId, tracking, payment.TransactionId!, "Order placed successfully.");
    }

    // ── CancelOrder ───────────────────────────────────────────
    // Orchestrates: cancel shipment → release stock → refund → notify → audit
    public OrderResult CancelOrder(Order order, string trackingNumber, string transactionId)
    {
        _audit.Log("CancelOrder", $"Started — order {order.OrderId}");

        // 1. Cancel shipment
        _shipping.CancelShipment(trackingNumber);

        // 2. Release reserved stock
        foreach (var line in order.Lines)
            _inventory.Release(line.ProductId, line.Quantity);

        // 3. Refund payment
        decimal total = order.Lines.Sum(l => l.LineTotal);
        _payment.Refund(transactionId, total);

        // 4. Notify customer
        _notifications.SendCancellationConfirmation(order.CustomerEmail, order.OrderId);
        _notifications.SendRefundConfirmation(order.CustomerEmail, order.OrderId, total);

        _audit.Log("CancelOrder", $"Completed — order {order.OrderId} refunded {total:C2}");
        return OrderResult.Ok(order.OrderId, null!, transactionId, "Order cancelled and refunded.");
    }
}
