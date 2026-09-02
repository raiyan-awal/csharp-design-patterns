using AntiCorruptionLayerPattern.Domain;
using AntiCorruptionLayerPattern.Gateway;
using AntiCorruptionLayerPattern.Legacy;
using AntiCorruptionLayerPattern.Services;
using AntiCorruptionLayerPattern.Translation;

// Composition root — wire the ACL stack together.
var legacyClient = new SimulatedLegacyFreightClient();
var translator   = new ShipmentTranslator();
IShipmentGateway gateway = new LegacyShipmentGateway(legacyClient, translator);
var freightService = new FreightService(gateway);

Console.WriteLine("=== 4.27 Anti-Corruption Layer — Maple Cargo Co. ===");
Console.WriteLine("Integrating with the legacy FREIGHTMASTER system\n");

// ─── 1. Retrieve existing shipments ──────────────────────────────────────────
Console.WriteLine("─── 1. Fetching existing shipments ───");
var all = freightService.GetAllShipments();
foreach (var s in all)
{
    Console.WriteLine($"  {s.Id} | {s.RecipientName,-20} | {s.Status,-10} | " +
                      $"{s.Package.WeightKg} kg | {s.Destination.City}, {s.Destination.Province}");
}

Pause();

// ─── 2. Active shipments only ─────────────────────────────────────────────────
Console.WriteLine("─── 2. Active shipments (Pending or InTransit) ───");
var active = freightService.GetActiveShipments();
Console.WriteLine($"  {active.Count} active shipment(s) found:");
foreach (var s in active)
    Console.WriteLine($"  {s.Id} | {s.RecipientName} | {s.Status} | Est. delivery: {s.EstimatedDelivery}");

Pause();

// ─── 3. Delivered check ──────────────────────────────────────────────────────
Console.WriteLine("─── 3. Delivered check ───");
Console.WriteLine($"  SHP-003 delivered? {freightService.IsDelivered("SHP-003")}");   // true
Console.WriteLine($"  SHP-001 delivered? {freightService.IsDelivered("SHP-001")}");   // false

Pause();

// ─── 4. Book a new shipment ──────────────────────────────────────────────────
Console.WriteLine("─── 4. Booking a new shipment ───");
var destination = new Address("483 Bay St", "Toronto", "ON", "M5G 2C9");
var package = new Dimensions(LengthCm: 30m, WidthCm: 20m, HeightCm: 15m, WeightKg: 3.5m);
var booked = freightService.BookShipment("Lena Beaumont", destination, package);

Console.WriteLine($"  Booked: {booked.Id}");
Console.WriteLine($"  Recipient : {booked.RecipientName}");
Console.WriteLine($"  Weight    : {booked.Package.WeightKg} kg  ({booked.Package.WeightKg / 0.453592m:F2} lbs in legacy)");
Console.WriteLine($"  Status    : {booked.Status}");
Console.WriteLine($"  Ships on  : {booked.ShippedOn}");
Console.WriteLine($"  Est. deliv: {booked.EstimatedDelivery}");

Pause();

// ─── 5. Domain model is clean — no legacy concepts ──────────────────────────
Console.WriteLine("─── 5. Domain model has no legacy fields ───");
Console.WriteLine("  FreightService, Shipment, Address, Dimensions contain:");
Console.WriteLine("  ✓ RecipientName (not RECIP_FIRST_NM / RECIP_LAST_NM)");
Console.WriteLine("  ✓ WeightKg (not WGT_LBS)");
Console.WriteLine("  ✓ LengthCm / WidthCm / HeightCm (not LEN_IN / WID_IN / HGT_IN)");
Console.WriteLine("  ✓ ShipmentStatus enum (not '01' / '02' / '03' / '09')");
Console.WriteLine("  ✓ DateOnly ShippedOn (not 'yyyyMMdd' string SHIP_DT)");
Console.WriteLine("\n  The ACL absorbs all FREIGHTMASTER complexity so the domain stays clean.");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
