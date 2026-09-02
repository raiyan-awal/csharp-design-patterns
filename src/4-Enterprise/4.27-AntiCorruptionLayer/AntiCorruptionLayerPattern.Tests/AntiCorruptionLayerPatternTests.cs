using AntiCorruptionLayerPattern.Domain;
using AntiCorruptionLayerPattern.Gateway;
using AntiCorruptionLayerPattern.Legacy;
using AntiCorruptionLayerPattern.Services;
using AntiCorruptionLayerPattern.Translation;
using Xunit;

namespace AntiCorruptionLayerPattern.Tests;

// ─── ShipmentTranslator — ToDomain ───────────────────────────────────────────

public class ShipmentTranslator_ToDomain_Tests
{
    private readonly ShipmentTranslator _translator = new();

    private static LegacyShipmentRecord BuildRecord(
        string shipId = "SHP-001",
        string statCd = "01",
        string first = "Jean", string last = "Tremblay",
        string addr = "200 Front St W", string city = "Toronto",
        string prov = "ON", string postal = "M5V 3K2", string ctry = "CA",
        decimal wgtLbs = 5.5m,
        decimal lenIn = 12m, decimal widIn = 8m, decimal hgtIn = 6m,
        string shipDt = "20260825", string? estDlvrDt = "20260901")
    => new()
    {
        SHIP_ID = shipId, STAT_CD = statCd,
        RECIP_FIRST_NM = first, RECIP_LAST_NM = last,
        ADDR_LINE1 = addr, CITY_NM = city, PROV_CD = prov,
        POSTAL_CD = postal, CTRY_CD = ctry,
        WGT_LBS = wgtLbs, LEN_IN = lenIn, WID_IN = widIn, HGT_IN = hgtIn,
        SHIP_DT = shipDt, EST_DLVR_DT = estDlvrDt
    };

    [Fact]
    public void MapsId_ToShipmentId()
    {
        var shipment = _translator.ToDomain(BuildRecord(shipId: "SHP-042"));
        Assert.Equal("SHP-042", shipment.Id);
    }

    [Fact]
    public void CombinesFirstAndLastName()
    {
        var shipment = _translator.ToDomain(BuildRecord(first: "Marie", last: "Gagnon"));
        Assert.Equal("Marie Gagnon", shipment.RecipientName);
    }

    [Fact]
    public void MapsAllAddressFields()
    {
        var shipment = _translator.ToDomain(BuildRecord(
            addr: "737 Granville St", city: "Vancouver",
            prov: "BC", postal: "V6Z 1G3", ctry: "CA"));

        Assert.Equal("737 Granville St", shipment.Destination.Street);
        Assert.Equal("Vancouver",        shipment.Destination.City);
        Assert.Equal("BC",               shipment.Destination.Province);
        Assert.Equal("V6Z 1G3",          shipment.Destination.PostalCode);
        Assert.Equal("CA",               shipment.Destination.Country);
    }

    [Theory]
    [InlineData("01", ShipmentStatus.Pending)]
    [InlineData("02", ShipmentStatus.InTransit)]
    [InlineData("03", ShipmentStatus.Delivered)]
    [InlineData("09", ShipmentStatus.Failed)]
    public void MapsStatusCode_ToEnum(string code, ShipmentStatus expected)
    {
        var shipment = _translator.ToDomain(BuildRecord(statCd: code));
        Assert.Equal(expected, shipment.Status);
    }

    [Fact]
    public void UnknownStatusCode_MapsToUnknown()
    {
        var shipment = _translator.ToDomain(BuildRecord(statCd: "99"));
        Assert.Equal(ShipmentStatus.Unknown, shipment.Status);
    }

    [Fact]
    public void ConvertsWeight_LbsToKg()
    {
        // 10 lbs × 0.453592 = 4.53592 → rounded to 2 dp = 4.54
        var shipment = _translator.ToDomain(BuildRecord(wgtLbs: 10m));
        Assert.Equal(4.54m, shipment.Package.WeightKg);
    }

    [Fact]
    public void ConvertsDimensions_InchesToCm()
    {
        // 10 inches × 2.54 = 25.40 cm
        var shipment = _translator.ToDomain(BuildRecord(lenIn: 10m, widIn: 10m, hgtIn: 10m));
        Assert.Equal(25.40m, shipment.Package.LengthCm);
        Assert.Equal(25.40m, shipment.Package.WidthCm);
        Assert.Equal(25.40m, shipment.Package.HeightCm);
    }

    [Fact]
    public void ParsesShipDate_FromyyyyMMdd()
    {
        var shipment = _translator.ToDomain(BuildRecord(shipDt: "20260815"));
        Assert.Equal(new DateOnly(2026, 8, 15), shipment.ShippedOn);
    }

    [Fact]
    public void ParsesEstimatedDelivery_WhenPresent()
    {
        var shipment = _translator.ToDomain(BuildRecord(estDlvrDt: "20260901"));
        Assert.Equal(new DateOnly(2026, 9, 1), shipment.EstimatedDelivery);
    }

    [Fact]
    public void EstimatedDelivery_IsNull_WhenAbsent()
    {
        var shipment = _translator.ToDomain(BuildRecord(estDlvrDt: null));
        Assert.Null(shipment.EstimatedDelivery);
    }
}

// ─── ShipmentTranslator — ToLegacy ───────────────────────────────────────────

public class ShipmentTranslator_ToLegacy_Tests
{
    private readonly ShipmentTranslator _translator = new();

    private static Address CdnAddress() =>
        new("483 Bay St", "Toronto", "ON", "M5G 2C9", "CA");

    private static Dimensions StdPackage() =>
        new(LengthCm: 25.4m, WidthCm: 25.4m, HeightCm: 25.4m, WeightKg: 4.53592m);

    [Fact]
    public void SplitsRecipientName_IntoFirstAndLast()
    {
        var req = _translator.ToLegacy("Lena Beaumont", CdnAddress(), StdPackage());
        Assert.Equal("Lena",    req.RECIP_FIRST_NM);
        Assert.Equal("Beaumont", req.RECIP_LAST_NM);
    }

    [Fact]
    public void SingleWordName_SetsLastNameEmpty()
    {
        var req = _translator.ToLegacy("Celine", CdnAddress(), StdPackage());
        Assert.Equal("Celine", req.RECIP_FIRST_NM);
        Assert.Equal("",       req.RECIP_LAST_NM);
    }

    [Fact]
    public void ConvertsWeight_KgToLbs()
    {
        // 4.53592 kg / 0.453592 = ~10.00 lbs
        var req = _translator.ToLegacy("Jean Tremblay", CdnAddress(), StdPackage());
        Assert.Equal(10.00m, req.WGT_LBS);
    }

    [Fact]
    public void ConvertsDimensions_CmToInches()
    {
        // 25.4 cm / 2.54 = 10.00 inches
        var req = _translator.ToLegacy("Jean Tremblay", CdnAddress(), StdPackage());
        Assert.Equal(10.00m, req.LEN_IN);
        Assert.Equal(10.00m, req.WID_IN);
        Assert.Equal(10.00m, req.HGT_IN);
    }

    [Fact]
    public void MapsAddressFields_ToLegacyNames()
    {
        var req = _translator.ToLegacy("Jean Tremblay", CdnAddress(), StdPackage());
        Assert.Equal("483 Bay St", req.ADDR_LINE1);
        Assert.Equal("Toronto",    req.CITY_NM);
        Assert.Equal("ON",         req.PROV_CD);
        Assert.Equal("M5G 2C9",    req.POSTAL_CD);
        Assert.Equal("CA",         req.CTRY_CD);
    }
}

// ─── LegacyShipmentGateway ───────────────────────────────────────────────────

public class LegacyShipmentGateway_Tests
{
    private static LegacyShipmentGateway BuildGateway() =>
        new(new SimulatedLegacyFreightClient(), new ShipmentTranslator());

    [Fact]
    public void GetShipment_ReturnsDomainObject_ForKnownId()
    {
        var gateway  = BuildGateway();
        var shipment = gateway.GetShipment("SHP-001");
        Assert.NotNull(shipment);
        Assert.Equal("SHP-001", shipment.Id);
    }

    [Fact]
    public void GetShipment_ReturnsNull_ForUnknownId()
    {
        var gateway = BuildGateway();
        Assert.Null(gateway.GetShipment("SHP-999"));
    }

    [Fact]
    public void GetShipment_TranslatesAllFields_Correctly()
    {
        var gateway  = BuildGateway();
        var shipment = gateway.GetShipment("SHP-001")!;

        Assert.Equal("Jean Tremblay",     shipment.RecipientName);
        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
        Assert.Equal("Toronto",           shipment.Destination.City);
        Assert.True(shipment.Package.WeightKg > 0);
        Assert.True(shipment.Package.LengthCm > 0);
    }

    [Fact]
    public void CreateShipment_ReturnsPendingShipment()
    {
        var gateway = BuildGateway();
        var dest    = new Address("100 Queen St W", "Toronto", "ON", "M5H 2N2");
        var pkg     = new Dimensions(30m, 20m, 15m, 2.5m);

        var shipment = gateway.CreateShipment("Amir Khalil", dest, pkg);

        Assert.NotEmpty(shipment.Id);
        Assert.Equal(ShipmentStatus.Pending, shipment.Status);
        Assert.Equal("Amir Khalil", shipment.RecipientName);
    }

    [Fact]
    public void GetStatus_ReturnsCorrectEnum_ForKnownId()
    {
        var gateway = BuildGateway();
        Assert.Equal(ShipmentStatus.Delivered, gateway.GetStatus("SHP-003"));
        Assert.Equal(ShipmentStatus.Failed,    gateway.GetStatus("SHP-004"));
    }

    [Fact]
    public void GetStatus_ReturnsUnknown_ForMissingId()
    {
        var gateway = BuildGateway();
        Assert.Equal(ShipmentStatus.Unknown, gateway.GetStatus("SHP-999"));
    }

    [Fact]
    public void GetAll_ReturnsAllSeededShipments()
    {
        var gateway = BuildGateway();
        Assert.Equal(4, gateway.GetAll().Count);
    }
}

// ─── FreightService ───────────────────────────────────────────────────────────

public class FreightService_Tests
{
    private static FreightService BuildService() =>
        new(new LegacyShipmentGateway(new SimulatedLegacyFreightClient(), new ShipmentTranslator()));

    [Fact]
    public void FindShipment_DelegatesToGateway()
    {
        var service  = BuildService();
        var shipment = service.FindShipment("SHP-002");
        Assert.NotNull(shipment);
        Assert.Equal("SHP-002", shipment.Id);
    }

    [Fact]
    public void FindShipment_ReturnsNull_WhenNotFound()
    {
        var service = BuildService();
        Assert.Null(service.FindShipment("SHP-MISSING"));
    }

    [Fact]
    public void BookShipment_CreatesNewShipment()
    {
        var service = BuildService();
        var dest    = new Address("1001 Rue de la Montagne", "Montreal", "QC", "H3G 1Z2");
        var pkg     = new Dimensions(40m, 30m, 20m, 5.0m);

        var shipment = service.BookShipment("Sophie Leblanc", dest, pkg);

        Assert.NotNull(shipment);
        Assert.Equal("Sophie Leblanc", shipment.RecipientName);
        Assert.Equal(ShipmentStatus.Pending, shipment.Status);
    }

    [Fact]
    public void BookShipment_Throws_WhenRecipientNameEmpty()
    {
        var service = BuildService();
        var dest    = new Address("100 St", "Toronto", "ON", "M5A 1A1");
        var pkg     = new Dimensions(10m, 10m, 10m, 1m);
        Assert.Throws<ArgumentException>(() => service.BookShipment("", dest, pkg));
    }

    [Fact]
    public void BookShipment_Throws_WhenWeightIsZero()
    {
        var service = BuildService();
        var dest    = new Address("100 St", "Toronto", "ON", "M5A 1A1");
        var pkg     = new Dimensions(10m, 10m, 10m, WeightKg: 0m);
        Assert.Throws<ArgumentException>(() => service.BookShipment("Jean Tremblay", dest, pkg));
    }

    [Fact]
    public void IsDelivered_ReturnsTrue_ForDeliveredShipment()
    {
        var service = BuildService();
        Assert.True(service.IsDelivered("SHP-003"));
    }

    [Fact]
    public void IsDelivered_ReturnsFalse_ForNonDelivered()
    {
        var service = BuildService();
        Assert.False(service.IsDelivered("SHP-001"));
    }

    [Fact]
    public void GetActiveShipments_ReturnsPendingAndInTransitOnly()
    {
        var service = BuildService();
        var active  = service.GetActiveShipments();

        Assert.All(active, s =>
            Assert.True(s.Status is ShipmentStatus.Pending or ShipmentStatus.InTransit));
    }

    [Fact]
    public void GetActiveShipments_ExcludesDeliveredAndFailed()
    {
        var service = BuildService();
        var active  = service.GetActiveShipments();

        Assert.DoesNotContain(active, s => s.Status == ShipmentStatus.Delivered);
        Assert.DoesNotContain(active, s => s.Status == ShipmentStatus.Failed);
    }
}

// ─── Integration ─────────────────────────────────────────────────────────────

public class Integration_Tests
{
    [Fact]
    public void CreateThenRetrieve_RoundTrip_PreservesData()
    {
        var gateway = new LegacyShipmentGateway(
            new SimulatedLegacyFreightClient(), new ShipmentTranslator());
        var service = new FreightService(gateway);

        var dest    = new Address("483 Bay St", "Toronto", "ON", "M5G 2C9");
        var pkg     = new Dimensions(LengthCm: 25.4m, WidthCm: 20m, HeightCm: 15m, WeightKg: 3.5m);
        var created = service.BookShipment("Lena Beaumont", dest, pkg);

        var fetched = service.FindShipment(created.Id);

        Assert.NotNull(fetched);
        Assert.Equal("Lena Beaumont", fetched.RecipientName);
        Assert.Equal("Toronto",       fetched.Destination.City);
        Assert.Equal("ON",            fetched.Destination.Province);
    }

    [Fact]
    public void DomainModel_ContainsNoLegacyFieldNames()
    {
        // Verifies that none of the domain types expose ALL_CAPS legacy field names.
        var shipmentProps = typeof(Shipment).GetProperties()
                                            .Select(p => p.Name)
                                            .ToList();

        Assert.DoesNotContain("SHIP_ID",       shipmentProps);
        Assert.DoesNotContain("STAT_CD",       shipmentProps);
        Assert.DoesNotContain("RECIP_FIRST_NM", shipmentProps);
        Assert.DoesNotContain("WGT_LBS",       shipmentProps);
        Assert.DoesNotContain("SHIP_DT",       shipmentProps);
    }
}
