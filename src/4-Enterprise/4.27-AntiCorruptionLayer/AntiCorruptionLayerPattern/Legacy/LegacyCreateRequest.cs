namespace AntiCorruptionLayerPattern.Legacy;

// Request payload sent to FREIGHTMASTER when creating a new shipment.
// Uses the same naming and imperial-unit conventions as LegacyShipmentRecord.
public sealed class LegacyCreateRequest
{
    public string RECIP_FIRST_NM { get; set; } = "";
    public string RECIP_LAST_NM { get; set; } = "";
    public string ADDR_LINE1 { get; set; } = "";
    public string CITY_NM { get; set; } = "";
    public string PROV_CD { get; set; } = "";
    public string POSTAL_CD { get; set; } = "";
    public string CTRY_CD { get; set; } = "";
    public decimal WGT_LBS { get; set; }
    public decimal LEN_IN { get; set; }
    public decimal WID_IN { get; set; }
    public decimal HGT_IN { get; set; }
}
