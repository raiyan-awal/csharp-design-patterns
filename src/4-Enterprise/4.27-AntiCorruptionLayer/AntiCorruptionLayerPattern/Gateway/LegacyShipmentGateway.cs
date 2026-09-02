using AntiCorruptionLayerPattern.Domain;
using AntiCorruptionLayerPattern.Legacy;
using AntiCorruptionLayerPattern.Translation;

namespace AntiCorruptionLayerPattern.Gateway;

// The ACL adapter: implements the clean IShipmentGateway using the legacy client
// plus the ShipmentTranslator. Domain services never see legacy types.
public sealed class LegacyShipmentGateway(
    ILegacyFreightClient client,
    ShipmentTranslator translator) : IShipmentGateway
{
    public Shipment? GetShipment(string shipmentId)
    {
        var record = client.FetchShipment(shipmentId);
        return record is null ? null : translator.ToDomain(record);
    }

    public Shipment CreateShipment(string recipientName, Address destination, Dimensions package)
    {
        var request  = translator.ToLegacy(recipientName, destination, package);
        var newId    = client.CreateShipment(request);
        var created  = client.FetchShipment(newId)!;
        return translator.ToDomain(created);
    }

    public ShipmentStatus GetStatus(string shipmentId)
    {
        var record = client.FetchShipment(shipmentId);
        return record is null ? ShipmentStatus.Unknown : ShipmentTranslator.MapStatus(record.STAT_CD);
    }

    public IReadOnlyList<Shipment> GetAll() =>
        client.FetchAll().Select(translator.ToDomain).ToList();
}
