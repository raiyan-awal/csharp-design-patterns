using AntiCorruptionLayerPattern.Domain;
using AntiCorruptionLayerPattern.Gateway;

namespace AntiCorruptionLayerPattern.Services;

// Domain service. Knows nothing about FREIGHTMASTER, status codes, or imperial units.
// All external complexity is hidden behind IShipmentGateway.
public sealed class FreightService(IShipmentGateway gateway)
{
    public Shipment? FindShipment(string shipmentId) =>
        gateway.GetShipment(shipmentId);

    public Shipment BookShipment(string recipientName, Address destination, Dimensions package)
    {
        if (string.IsNullOrWhiteSpace(recipientName))
            throw new ArgumentException("Recipient name is required.", nameof(recipientName));
        if (package.WeightKg <= 0)
            throw new ArgumentException("Package weight must be positive.", nameof(package));

        return gateway.CreateShipment(recipientName, destination, package);
    }

    public bool IsDelivered(string shipmentId) =>
        gateway.GetStatus(shipmentId) == ShipmentStatus.Delivered;

    public IReadOnlyList<Shipment> GetActiveShipments() =>
        gateway.GetAll()
               .Where(s => s.Status is ShipmentStatus.Pending or ShipmentStatus.InTransit)
               .ToList();

    public IReadOnlyList<Shipment> GetAllShipments() =>
        gateway.GetAll();
}
