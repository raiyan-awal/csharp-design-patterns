using AntiCorruptionLayerPattern.Domain;
using AntiCorruptionLayerPattern.Legacy;

namespace AntiCorruptionLayerPattern.Translation;

// The core of the Anti-Corruption Layer: bidirectional translation between
// the legacy FREIGHTMASTER model and our clean domain model.
// All imperial/metric conversions and code mappings live here so they
// never leak into domain services or the gateway interface.
public sealed class ShipmentTranslator
{
    private const decimal CmPerInch    = 2.54m;
    private const decimal KgPerLb      = 0.453592m;

    public Shipment ToDomain(LegacyShipmentRecord r) => new()
    {
        Id              = r.SHIP_ID,
        RecipientName   = $"{r.RECIP_FIRST_NM} {r.RECIP_LAST_NM}".Trim(),
        Destination     = new Address(r.ADDR_LINE1, r.CITY_NM, r.PROV_CD, r.POSTAL_CD, r.CTRY_CD),
        Package         = new Dimensions(
                            LengthCm  : Math.Round(r.LEN_IN * CmPerInch, 2),
                            WidthCm   : Math.Round(r.WID_IN * CmPerInch, 2),
                            HeightCm  : Math.Round(r.HGT_IN * CmPerInch, 2),
                            WeightKg  : Math.Round(r.WGT_LBS * KgPerLb, 2)),
        Status          = MapStatus(r.STAT_CD),
        ShippedOn       = DateOnly.ParseExact(r.SHIP_DT, "yyyyMMdd"),
        EstimatedDelivery = r.EST_DLVR_DT is not null
                            ? DateOnly.ParseExact(r.EST_DLVR_DT, "yyyyMMdd")
                            : null
    };

    public LegacyCreateRequest ToLegacy(string recipientName, Address destination, Dimensions package)
    {
        var parts = recipientName.Split(' ', 2);
        return new LegacyCreateRequest
        {
            RECIP_FIRST_NM = parts[0],
            RECIP_LAST_NM  = parts.Length > 1 ? parts[1] : "",
            ADDR_LINE1     = destination.Street,
            CITY_NM        = destination.City,
            PROV_CD        = destination.Province,
            POSTAL_CD      = destination.PostalCode,
            CTRY_CD        = destination.Country,
            WGT_LBS        = Math.Round(package.WeightKg / KgPerLb, 2),
            LEN_IN         = Math.Round(package.LengthCm / CmPerInch, 2),
            WID_IN         = Math.Round(package.WidthCm  / CmPerInch, 2),
            HGT_IN         = Math.Round(package.HeightCm / CmPerInch, 2)
        };
    }

    public static ShipmentStatus MapStatus(string code) => code switch
    {
        "01" => ShipmentStatus.Pending,
        "02" => ShipmentStatus.InTransit,
        "03" => ShipmentStatus.Delivered,
        "09" => ShipmentStatus.Failed,
        _    => ShipmentStatus.Unknown
    };
}
