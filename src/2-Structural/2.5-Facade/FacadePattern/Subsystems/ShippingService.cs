namespace FacadePattern;

// ============================================================
// SUBSYSTEM: Shipping
// ============================================================
// Creates and cancels shipments. Has no knowledge of payments,
// inventory, or any other subsystem.

public sealed class ShippingService
{
    public string CreateShipment(string address, IEnumerable<OrderLine> lines)
    {
        string tracking = $"TRK-{Guid.NewGuid():N}"[..12].ToUpper();
        Console.WriteLine($"[SHIPPING] Shipment created — tracking: {tracking}");
        Console.WriteLine($"[SHIPPING] Destination: {address}");
        foreach (var line in lines)
            Console.WriteLine($"[SHIPPING]   {line.Quantity}× {line.ProductName}");
        return tracking;
    }

    public void CancelShipment(string trackingNumber)
        => Console.WriteLine($"[SHIPPING] Shipment {trackingNumber} cancelled.");
}
