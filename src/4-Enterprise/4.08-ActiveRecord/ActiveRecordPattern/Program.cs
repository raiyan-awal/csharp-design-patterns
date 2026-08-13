using Microsoft.Data.Sqlite;
using ActiveRecordPattern.Infrastructure;
using ActiveRecordPattern.Records;

var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();
Database.Initialize(connection);
Schema.Create();

// ── Section 1: Create rental units ───────────────────────────────────────────
Console.WriteLine("=== Maple Ridge Realty — Active Record Demo ===\n");
Console.WriteLine("--- Creating Rental Units ---");

var unit1 = new RentalUnit("221 King St W, Apt 4A", "Toronto", "ON", 2_400m, 1);
unit1.Save();
var unit2 = new RentalUnit("885 Robson St, Suite 12", "Vancouver", "BC", 2_800m, 2);
unit2.Save();
var unit3 = new RentalUnit("107 Elgin St, Unit 3", "Ottawa", "ON", 1_850m, 1);
unit3.Save();
var unit4 = new RentalUnit("1442 Sherbrooke St W, Apt 6", "Montreal", "QC", 1_600m, 2);
unit4.Save();

Console.WriteLine($"Saved {RentalUnit.FindAll().Count} units to database.\n");
foreach (var u in RentalUnit.FindAll())
    Console.WriteLine($"  [{u.Id}] {u.Address}, {u.City} — ${u.MonthlyRent:N0}/mo · {u.Bedrooms}BR · {(u.IsAvailable ? "Available" : "Rented")}");

Pause();

// ── Section 2: Query units ───────────────────────────────────────────────────
Console.WriteLine("--- Querying Units ---");
Console.WriteLine($"\nAll available units ({RentalUnit.FindAvailable().Count}):");
foreach (var u in RentalUnit.FindAvailable())
    Console.WriteLine($"  ${u.MonthlyRent:N0}/mo — {u.Address}, {u.City}");

Console.WriteLine($"\nToronto listings:");
foreach (var u in RentalUnit.FindByCity("Toronto"))
    Console.WriteLine($"  {u.Address} — {u.Bedrooms}BR at ${u.MonthlyRent:N0}/mo");

Pause();

// ── Section 3: Rent units (domain behaviour + auto-save) ─────────────────────
Console.WriteLine("--- Renting Units ---");
unit1.Rent();
unit2.Rent();
Console.WriteLine($"Rented unit1 and unit2.");
Console.WriteLine($"Available units remaining: {RentalUnit.FindAvailable().Count}");
Console.WriteLine($"'{unit1.Address}' available? {unit1.IsAvailable}");

Console.WriteLine("\nAttempting to double-rent unit1:");
try
{
    unit1.Rent();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"  Caught: {ex.Message}");
}

Pause();

// ── Section 4: Update rent & vacate ─────────────────────────────────────────
Console.WriteLine("--- Updating Rent & Vacating ---");
unit2.UpdateRent(2_950m);
Console.WriteLine($"Vancouver rent updated to ${unit2.MonthlyRent:N0}/mo");

unit1.Vacate();
Console.WriteLine($"'{unit1.Address}' vacated — available again: {unit1.IsAvailable}");

var reloaded = RentalUnit.FindById(unit1.Id)!;
Console.WriteLine($"Reloaded from DB — available: {reloaded.IsAvailable}");
Console.WriteLine($"Available units: {RentalUnit.FindAvailable().Count}");

Pause();

// ── Section 5: Tenants ───────────────────────────────────────────────────────
Console.WriteLine("--- Managing Tenants ---");

var leaseStart = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc);
var leaseEnd   = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc);

var tenant1 = new Tenant("Sophie Tremblay", "sophie@example.ca", "514-555-0101",
                         unit2.Id, leaseStart, leaseEnd);
tenant1.Save();

unit3.Rent();
var tenant2 = new Tenant("Aiden Kowalski", "aiden@example.ca", "613-555-0182",
                         unit3.Id, leaseStart, leaseEnd);
tenant2.Save();

Console.WriteLine("Tenants saved:");
foreach (var t in Tenant.FindAll())
    Console.WriteLine($"  [{t.Id}] {t.Name} — Unit #{t.RentalUnitId}, lease ends {t.LeaseEnd:yyyy-MM-dd}");

Console.WriteLine($"\nTenants in Vancouver unit (#{unit2.Id}): {Tenant.FindByUnit(unit2.Id).Count}");

Pause();

// ── Section 6: Extend lease & delete ────────────────────────────────────────
Console.WriteLine("--- Extending Lease & Cleanup ---");
tenant1.ExtendLease(6);
Console.WriteLine($"Sophie's new lease end: {tenant1.LeaseEnd:yyyy-MM-dd}");

var reloadedTenant = Tenant.FindById(tenant1.Id)!;
Console.WriteLine($"Reloaded from DB:        {reloadedTenant.LeaseEnd:yyyy-MM-dd}");

tenant2.Delete();
Console.WriteLine($"\nAfter deleting Aiden: {Tenant.FindAll().Count} tenant(s) remaining");

unit4.Delete();
Console.WriteLine($"After deleting Montreal unit: {RentalUnit.FindAll().Count} units remaining");

connection.Dispose();

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
