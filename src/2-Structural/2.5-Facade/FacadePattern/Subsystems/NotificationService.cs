namespace FacadePattern;

// ============================================================
// SUBSYSTEM: Notifications
// ============================================================
// Sends customer emails. Has no knowledge of payments,
// inventory, shipping, or any other subsystem.

public sealed class NotificationService
{
    public void SendOrderConfirmation(string email, string orderId, string trackingNumber)
    {
        Console.WriteLine($"[EMAIL] Order confirmation → {email}");
        Console.WriteLine($"        Order: {orderId}  |  Tracking: {trackingNumber}");
    }

    public void SendCancellationConfirmation(string email, string orderId)
        => Console.WriteLine($"[EMAIL] Cancellation notice → {email} for order {orderId}");

    public void SendRefundConfirmation(string email, string orderId, decimal amount)
        => Console.WriteLine($"[EMAIL] Refund notice → {email} for order {orderId} ({amount:C2})");
}
