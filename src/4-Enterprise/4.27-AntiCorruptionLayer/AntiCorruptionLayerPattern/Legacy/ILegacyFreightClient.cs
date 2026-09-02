namespace AntiCorruptionLayerPattern.Legacy;

public interface ILegacyFreightClient
{
    LegacyShipmentRecord? FetchShipment(string shipId);
    string CreateShipment(LegacyCreateRequest request);
    void UpdateStatus(string shipId, string statusCode);
    IReadOnlyList<LegacyShipmentRecord> FetchAll();
}
