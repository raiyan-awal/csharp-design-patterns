namespace AntiCorruptionLayerPattern.Legacy;

// Raw data structure returned by the FREIGHTMASTER legacy system.
// Field names follow the legacy naming convention (ALL_CAPS abbreviations).
// Units are imperial (lbs, inches). Dates are "yyyyMMdd" strings.
public sealed class LegacyShipmentRecord
{
    public string SHIP_ID { get; set; } = "";
    public string STAT_CD { get; set; } = "";   // "01"=Pending "02"=InTransit "03"=Delivered "09"=Failed
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
    public string SHIP_DT { get; set; } = "";       // "yyyyMMdd"
    public string? EST_DLVR_DT { get; set; }        // "yyyyMMdd" or null
}
