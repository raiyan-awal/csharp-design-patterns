using Microsoft.Data.Sqlite;
using ActiveRecordPattern.Infrastructure;
using ActiveRecordPattern.Records;

namespace ActiveRecordPattern.Tests;

public sealed class ActiveRecordTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public ActiveRecordTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        Database.Initialize(_connection);
        Schema.Create();
    }

    public void Dispose() => _connection.Dispose();

    // ── RentalUnit ────────────────────────────────────────────────────────────

    [Fact]
    public void Save_NewUnit_AssignsId()
    {
        var unit = new RentalUnit("101 Main St", "Calgary", "AB", 1_500m, 1);
        Assert.Equal(0, unit.Id);

        unit.Save();

        Assert.True(unit.Id > 0);
    }

    [Fact]
    public void FindById_ReturnsUnit_WhenExists()
    {
        var unit = new RentalUnit("202 Oak Ave", "Edmonton", "AB", 1_200m, 1);
        unit.Save();

        var found = RentalUnit.FindById(unit.Id);

        Assert.NotNull(found);
        Assert.Equal("202 Oak Ave", found.Address);
        Assert.Equal("Edmonton", found.City);
        Assert.Equal("AB", found.Province);
        Assert.Equal(1_200m, found.MonthlyRent);
        Assert.Equal(1, found.Bedrooms);
        Assert.True(found.IsAvailable);
    }

    [Fact]
    public void FindById_ReturnsNull_WhenNotFound()
    {
        var result = RentalUnit.FindById(9999);

        Assert.Null(result);
    }

    [Fact]
    public void FindAll_ReturnsAllUnits()
    {
        new RentalUnit("1 A St", "Toronto", "ON", 2_000m, 1).Save();
        new RentalUnit("2 B St", "Toronto", "ON", 2_200m, 2).Save();
        new RentalUnit("3 C St", "Vancouver", "BC", 2_500m, 1).Save();

        var all = RentalUnit.FindAll();

        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void FindAvailable_ExcludesRentedUnits()
    {
        var unit1 = new RentalUnit("10 Park Rd", "Winnipeg", "MB", 1_100m, 1);
        unit1.Save();
        var unit2 = new RentalUnit("20 Lake Rd", "Winnipeg", "MB", 1_300m, 2);
        unit2.Save();
        unit1.Rent();

        var available = RentalUnit.FindAvailable();

        Assert.Single(available);
        Assert.Equal(unit2.Id, available[0].Id);
    }

    [Fact]
    public void FindByCity_ReturnsMatchingUnits()
    {
        new RentalUnit("5 First St", "Halifax", "NS", 1_400m, 1).Save();
        new RentalUnit("6 Second St", "Halifax", "NS", 1_600m, 2).Save();
        new RentalUnit("7 Third St", "Moncton", "NB", 1_000m, 1).Save();

        var halifaxUnits = RentalUnit.FindByCity("Halifax");

        Assert.Equal(2, halifaxUnits.Count);
        Assert.All(halifaxUnits, u => Assert.Equal("Halifax", u.City));
    }

    [Fact]
    public void Save_Update_PersistsChanges()
    {
        var unit = new RentalUnit("300 Elm St", "Regina", "SK", 950m, 1);
        unit.Save();

        unit.UpdateRent(1_050m);
        var reloaded = RentalUnit.FindById(unit.Id)!;

        Assert.Equal(1_050m, reloaded.MonthlyRent);
    }

    [Fact]
    public void Delete_RemovesUnit()
    {
        var unit = new RentalUnit("400 Pine St", "Saskatoon", "SK", 1_100m, 2);
        unit.Save();
        var id = unit.Id;

        unit.Delete();

        Assert.Null(RentalUnit.FindById(id));
    }

    [Fact]
    public void Rent_SetsIsAvailableFalse_AndPersists()
    {
        var unit = new RentalUnit("500 Cedar Ave", "Victoria", "BC", 2_100m, 1);
        unit.Save();

        unit.Rent();

        Assert.False(unit.IsAvailable);
        Assert.False(RentalUnit.FindById(unit.Id)!.IsAvailable);
    }

    [Fact]
    public void Rent_Throws_WhenAlreadyRented()
    {
        var unit = new RentalUnit("600 Maple Dr", "Kelowna", "BC", 1_800m, 2);
        unit.Save();
        unit.Rent();

        var ex = Assert.Throws<InvalidOperationException>(() => unit.Rent());
        Assert.Contains("already rented", ex.Message);
    }

    [Fact]
    public void Vacate_SetsIsAvailableTrue_AndPersists()
    {
        var unit = new RentalUnit("700 Birch Blvd", "London", "ON", 1_700m, 1);
        unit.Save();
        unit.Rent();

        unit.Vacate();

        Assert.True(unit.IsAvailable);
        Assert.True(RentalUnit.FindById(unit.Id)!.IsAvailable);
    }

    [Fact]
    public void UpdateRent_ChangesMonthlyRent_AndPersists()
    {
        var unit = new RentalUnit("800 Spruce Cres", "Windsor", "ON", 1_300m, 1);
        unit.Save();

        unit.UpdateRent(1_450m);

        Assert.Equal(1_450m, unit.MonthlyRent);
        Assert.Equal(1_450m, RentalUnit.FindById(unit.Id)!.MonthlyRent);
    }

    [Fact]
    public void UpdateRent_Throws_WhenZeroOrNegative()
    {
        var unit = new RentalUnit("900 Fir Lane", "Sudbury", "ON", 1_000m, 1);
        unit.Save();

        Assert.Throws<ArgumentException>(() => unit.UpdateRent(0m));
        Assert.Throws<ArgumentException>(() => unit.UpdateRent(-100m));
    }

    // ── Tenant ────────────────────────────────────────────────────────────────

    private static (DateTime start, DateTime end) StandardLease() =>
        (new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc),
         new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc));

    private RentalUnit SavedUnit()
    {
        var u = new RentalUnit("Test St", "Toronto", "ON", 2_000m, 1);
        u.Save();
        return u;
    }

    [Fact]
    public void SaveTenant_NewTenant_AssignsId()
    {
        var unit = SavedUnit();
        var (start, end) = StandardLease();
        var tenant = new Tenant("Emily Chen", "emily@example.ca", "416-555-0001", unit.Id, start, end);
        Assert.Equal(0, tenant.Id);

        tenant.Save();

        Assert.True(tenant.Id > 0);
    }

    [Fact]
    public void FindTenantById_ReturnsTenant_WhenExists()
    {
        var unit = SavedUnit();
        var (start, end) = StandardLease();
        var tenant = new Tenant("Liam Bouchard", "liam@example.ca", "514-555-0002", unit.Id, start, end);
        tenant.Save();

        var found = Tenant.FindById(tenant.Id);

        Assert.NotNull(found);
        Assert.Equal("Liam Bouchard", found.Name);
        Assert.Equal("liam@example.ca", found.Email);
        Assert.Equal(unit.Id, found.RentalUnitId);
        Assert.Equal(end, found.LeaseEnd);
    }

    [Fact]
    public void FindAllTenants_ReturnsAllTenants()
    {
        var unit = SavedUnit();
        var (start, end) = StandardLease();
        new Tenant("A Tenant", "a@example.ca", "111-111-1111", unit.Id, start, end).Save();
        new Tenant("B Tenant", "b@example.ca", "222-222-2222", unit.Id, start, end).Save();

        var all = Tenant.FindAll();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void FindByUnit_ReturnsTenantsForUnit()
    {
        var unit1 = SavedUnit();
        var unit2 = SavedUnit();
        var (start, end) = StandardLease();
        new Tenant("Tenant A", "a@x.ca", "000-000-0001", unit1.Id, start, end).Save();
        new Tenant("Tenant B", "b@x.ca", "000-000-0002", unit1.Id, start, end).Save();
        new Tenant("Tenant C", "c@x.ca", "000-000-0003", unit2.Id, start, end).Save();

        var unit1Tenants = Tenant.FindByUnit(unit1.Id);

        Assert.Equal(2, unit1Tenants.Count);
        Assert.All(unit1Tenants, t => Assert.Equal(unit1.Id, t.RentalUnitId));
    }

    [Fact]
    public void ExtendLease_UpdatesLeaseEnd_AndPersists()
    {
        var unit = SavedUnit();
        var (start, end) = StandardLease();
        var tenant = new Tenant("Noah Diaz", "noah@example.ca", "604-555-0003", unit.Id, start, end);
        tenant.Save();

        tenant.ExtendLease(6);

        var expected = end.AddMonths(6);
        Assert.Equal(expected, tenant.LeaseEnd);
        Assert.Equal(expected, Tenant.FindById(tenant.Id)!.LeaseEnd);
    }

    [Fact]
    public void ExtendLease_Throws_WhenZeroMonths()
    {
        var unit = SavedUnit();
        var (start, end) = StandardLease();
        var tenant = new Tenant("Olivia Park", "olivia@example.ca", "780-555-0004", unit.Id, start, end);
        tenant.Save();

        Assert.Throws<ArgumentException>(() => tenant.ExtendLease(0));
        Assert.Throws<ArgumentException>(() => tenant.ExtendLease(-1));
    }

    [Fact]
    public void DeleteTenant_RemovesTenant()
    {
        var unit = SavedUnit();
        var (start, end) = StandardLease();
        var tenant = new Tenant("James Liu", "james@example.ca", "902-555-0005", unit.Id, start, end);
        tenant.Save();
        var id = tenant.Id;

        tenant.Delete();

        Assert.Null(Tenant.FindById(id));
    }

    [Fact]
    public void TenantConstructor_Throws_WhenLeaseEndNotAfterLeaseStart()
    {
        var unit = SavedUnit();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end   = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(
            () => new Tenant("Bad Tenant", "bad@x.ca", "000-000-0000", unit.Id, start, end));
    }
}
