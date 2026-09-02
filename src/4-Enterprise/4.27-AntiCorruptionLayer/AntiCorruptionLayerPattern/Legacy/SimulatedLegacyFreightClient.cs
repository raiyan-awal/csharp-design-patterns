namespace AntiCorruptionLayerPattern.Legacy;

// In-memory stand-in for the real FREIGHTMASTER HTTP/SOAP client.
// Pre-seeded with Canadian shipments so the demo and tests run without a real endpoint.
public sealed class SimulatedLegacyFreightClient : ILegacyFreightClient
{
    private readonly Dictionary<string, LegacyShipmentRecord> _store = new();
    private int _nextId = 1000;

    public SimulatedLegacyFreightClient()
    {
        Seed("SHP-001", "02", "Jean", "Tremblay",
            "200 Front St W", "Toronto", "ON", "M5V 3K2", "CA",
            5.5m, 12m, 8m, 6m, "20260825", "20260901");

        Seed("SHP-002", "01", "Marie", "Gagnon",
            "1001 Rue de la Montagne", "Montreal", "QC", "H3G 1Z2", "CA",
            2.2m, 9m, 6m, 4m, "20260901", "20260906");

        Seed("SHP-003", "03", "Amir", "Khalil",
            "737 Granville St", "Vancouver", "BC", "V6Z 1G3", "CA",
            8.0m, 18m, 12m, 10m, "20260810", "20260820");

        Seed("SHP-004", "09", "Sarah", "MacDonald",
            "1801 Hollis St", "Halifax", "NS", "B3J 3N4", "CA",
            1.5m, 6m, 4m, 3m, "20260828", null);
    }

    public LegacyShipmentRecord? FetchShipment(string shipId) =>
        _store.TryGetValue(shipId, out var rec) ? rec : null;

    public string CreateShipment(LegacyCreateRequest request)
    {
        var id = $"SHP-{++_nextId}";
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        _store[id] = new LegacyShipmentRecord
        {
            SHIP_ID      = id,
            STAT_CD      = "01",
            RECIP_FIRST_NM = request.RECIP_FIRST_NM,
            RECIP_LAST_NM  = request.RECIP_LAST_NM,
            ADDR_LINE1   = request.ADDR_LINE1,
            CITY_NM      = request.CITY_NM,
            PROV_CD      = request.PROV_CD,
            POSTAL_CD    = request.POSTAL_CD,
            CTRY_CD      = request.CTRY_CD,
            WGT_LBS      = request.WGT_LBS,
            LEN_IN       = request.LEN_IN,
            WID_IN       = request.WID_IN,
            HGT_IN       = request.HGT_IN,
            SHIP_DT      = today.ToString("yyyyMMdd"),
            EST_DLVR_DT  = today.AddDays(5).ToString("yyyyMMdd")
        };
        return id;
    }

    public void UpdateStatus(string shipId, string statusCode)
    {
        if (_store.TryGetValue(shipId, out var rec))
            _store[shipId] = new LegacyShipmentRecord
            {
                SHIP_ID      = rec.SHIP_ID,
                STAT_CD      = statusCode,
                RECIP_FIRST_NM = rec.RECIP_FIRST_NM,
                RECIP_LAST_NM  = rec.RECIP_LAST_NM,
                ADDR_LINE1   = rec.ADDR_LINE1,
                CITY_NM      = rec.CITY_NM,
                PROV_CD      = rec.PROV_CD,
                POSTAL_CD    = rec.POSTAL_CD,
                CTRY_CD      = rec.CTRY_CD,
                WGT_LBS      = rec.WGT_LBS,
                LEN_IN       = rec.LEN_IN,
                WID_IN       = rec.WID_IN,
                HGT_IN       = rec.HGT_IN,
                SHIP_DT      = rec.SHIP_DT,
                EST_DLVR_DT  = rec.EST_DLVR_DT
            };
    }

    public IReadOnlyList<LegacyShipmentRecord> FetchAll() => [.._store.Values];

    private void Seed(string id, string status,
        string first, string last, string addr, string city,
        string prov, string postal, string ctry,
        decimal wgtLbs, decimal lenIn, decimal widIn, decimal hgtIn,
        string shipDt, string? estDlvrDt)
    {
        _store[id] = new LegacyShipmentRecord
        {
            SHIP_ID      = id,
            STAT_CD      = status,
            RECIP_FIRST_NM = first,
            RECIP_LAST_NM  = last,
            ADDR_LINE1   = addr,
            CITY_NM      = city,
            PROV_CD      = prov,
            POSTAL_CD    = postal,
            CTRY_CD      = ctry,
            WGT_LBS      = wgtLbs,
            LEN_IN       = lenIn,
            WID_IN       = widIn,
            HGT_IN       = hgtIn,
            SHIP_DT      = shipDt,
            EST_DLVR_DT  = estDlvrDt
        };
    }
}
