using AntiCorruptionLayerPattern.Domain;

namespace AntiCorruptionLayerPattern.Gateway;

// The clean port our domain talks to. Every method speaks pure domain language:
// no status codes, no imperial units, no YYYYMMDD strings, no legacy field names.
public interface IShipmentGateway
{
    Shipment? GetShipment(string shipmentId);
    Shipment CreateShipment(string recipientName, Address destination, Dimensions package);
    ShipmentStatus GetStatus(string shipmentId);
    IReadOnlyList<Shipment> GetAll();
}
