# 4.27 — Anti-Corruption Layer

## Intent

The Anti-Corruption Layer (ACL) is a translation boundary between your clean domain model and an external or legacy system whose terminology, data formats, and conventions differ from yours. Instead of letting the external model's concepts leak into your domain, the ACL intercepts every exchange and translates both directions — incoming responses are converted into your domain types, and outgoing requests are converted into whatever shape the external system expects. Your domain code speaks only its own language.

## The Problem It Solves

Without an ACL, every service that needs data from the legacy FREIGHTMASTER system must decode its conventions directly:

```csharp
// Without ACL: legacy concepts leak everywhere
public class ShippingReport
{
    public void PrintSummary(LegacyShipmentRecord r)
    {
        var kg = r.WGT_LBS * 0.453592m;                           // imperial conversion in domain
        var status = r.STAT_CD == "02" ? "In Transit" : "Other"; // magic status codes in domain
        var shipped = DateOnly.ParseExact(r.SHIP_DT, "yyyyMMdd"); // date format in domain
        var name = $"{r.RECIP_FIRST_NM} {r.RECIP_LAST_NM}";      // split name in domain
        Console.WriteLine($"{name} | {status} | {kg} kg | shipped {shipped}");
    }
}
```

Problems this creates:
- **Domain pollution** — every class that touches the legacy system carries its noise: `STAT_CD`, `WGT_LBS`, `yyyyMMdd`, `RECIP_FIRST_NM`. Change anything in the legacy system and edits scatter across the codebase.
- **Duplicated conversion logic** — kg/lb conversions, status-code mappings, and date parsing are repeated in every consumer instead of being defined once.
- **Untestable domain logic** — business rules become entangled with format translations; you cannot test one without the other.
- **Conceptual mismatch** — the legacy system may not even have a concept that maps cleanly to your domain. Forcing a direct mapping produces domain models that are twisted to fit external conventions.

## Solution: A Translation Boundary

The ACL introduces three collaborating pieces: a **translator** that maps between models, a **gateway interface** that exposes only domain types, and a **gateway implementation** that wires the translator to the legacy client. Domain services call only the clean interface and never see a legacy type.

```csharp
// Domain service: zero knowledge of FREIGHTMASTER
public sealed class FreightService(IShipmentGateway gateway)
{
    public Shipment? FindShipment(string id)       => gateway.GetShipment(id);
    public bool      IsDelivered(string id)        => gateway.GetStatus(id) == ShipmentStatus.Delivered;
    public IReadOnlyList<Shipment> GetActive()     =>
        gateway.GetAll().Where(s => s.Status is Pending or InTransit).ToList();
}
```

The gateway itself delegates all translation to `ShipmentTranslator`:

```csharp
public sealed class LegacyShipmentGateway(ILegacyFreightClient client, ShipmentTranslator translator)
    : IShipmentGateway
{
    public Shipment? GetShipment(string id)
    {
        var record = client.FetchShipment(id);       // returns LegacyShipmentRecord
        return record is null ? null : translator.ToDomain(record);  // domain only exits here
    }
}
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Domain model | `Shipment`, `Address`, `Dimensions`, `ShipmentStatus` | Clean domain types; no legacy fields or conventions |
| Gateway interface | `IShipmentGateway` | The port domain services depend on; speaks pure domain |
| Gateway (ACL adapter) | `LegacyShipmentGateway` | Implements the clean interface; coordinates client + translator |
| Translator | `ShipmentTranslator` | Bidirectional mapping — `ToDomain` and `ToLegacy`; all conversions live here |
| Legacy client interface | `ILegacyFreightClient` | Abstracts the FREIGHTMASTER HTTP/SOAP transport |
| Legacy client | `SimulatedLegacyFreightClient` | In-memory stand-in for the real legacy endpoint |
| Legacy DTOs | `LegacyShipmentRecord`, `LegacyCreateRequest` | Raw FREIGHTMASTER data structures; stay inside the ACL |
| Domain service | `FreightService` | Uses `IShipmentGateway`; never imports anything from the `Legacy` namespace |

## Structure

```
4.27-AntiCorruptionLayer/
├── AntiCorruptionLayerPattern/
│   ├── Domain/
│   │   ├── Shipment.cs              ← domain entity; RecipientName, Dimensions, ShipmentStatus, DateOnly
│   │   ├── ShipmentStatus.cs        ← enum: Pending | InTransit | Delivered | Failed | Unknown
│   │   ├── Address.cs               ← record: Street, City, Province, PostalCode, Country
│   │   └── Dimensions.cs            ← record: LengthCm, WidthCm, HeightCm, WeightKg
│   ├── Gateway/
│   │   ├── IShipmentGateway.cs      ← clean port; domain language only
│   │   └── LegacyShipmentGateway.cs ← ACL adapter; wires translator + client
│   ├── Legacy/
│   │   ├── LegacyShipmentRecord.cs  ← raw FREIGHTMASTER record (ALL_CAPS, imperial units)
│   │   ├── LegacyCreateRequest.cs   ← raw create payload for FREIGHTMASTER
│   │   ├── ILegacyFreightClient.cs  ← transport interface
│   │   └── SimulatedLegacyFreightClient.cs ← in-memory fake; pre-seeded Canadian shipments
│   ├── Translation/
│   │   └── ShipmentTranslator.cs    ← core ACL: ToDomain + ToLegacy; all conversions here
│   ├── Services/
│   │   └── FreightService.cs        ← domain service; no Legacy imports
│   └── Program.cs
└── AntiCorruptionLayerPattern.Tests/
    └── AntiCorruptionLayerPatternTests.cs  ← 36 tests across 5 suites
```

## Key Code

### ShipmentTranslator — the core of the ACL

```csharp
public sealed class ShipmentTranslator
{
    private const decimal CmPerInch = 2.54m;
    private const decimal KgPerLb   = 0.453592m;

    public Shipment ToDomain(LegacyShipmentRecord r) => new()
    {
        Id            = r.SHIP_ID,
        RecipientName = $"{r.RECIP_FIRST_NM} {r.RECIP_LAST_NM}".Trim(),
        Destination   = new Address(r.ADDR_LINE1, r.CITY_NM, r.PROV_CD, r.POSTAL_CD, r.CTRY_CD),
        Package       = new Dimensions(
                          LengthCm : Math.Round(r.LEN_IN * CmPerInch, 2),
                          WidthCm  : Math.Round(r.WID_IN * CmPerInch, 2),
                          HeightCm : Math.Round(r.HGT_IN * CmPerInch, 2),
                          WeightKg : Math.Round(r.WGT_LBS * KgPerLb, 2)),
        Status        = MapStatus(r.STAT_CD),
        ShippedOn     = DateOnly.ParseExact(r.SHIP_DT, "yyyyMMdd"),
        EstimatedDelivery = r.EST_DLVR_DT is not null
                            ? DateOnly.ParseExact(r.EST_DLVR_DT, "yyyyMMdd") : null
    };

    public static ShipmentStatus MapStatus(string code) => code switch
    {
        "01" => ShipmentStatus.Pending,
        "02" => ShipmentStatus.InTransit,
        "03" => ShipmentStatus.Delivered,
        "09" => ShipmentStatus.Failed,
        _    => ShipmentStatus.Unknown
    };
}
```

Every conversion — imperial-to-metric, status code to enum, `yyyyMMdd` to `DateOnly`, split name fields to a single string — lives in this one class. Domain services import none of it.

### IShipmentGateway — the clean port

```csharp
public interface IShipmentGateway
{
    Shipment? GetShipment(string shipmentId);
    Shipment  CreateShipment(string recipientName, Address destination, Dimensions package);
    ShipmentStatus GetStatus(string shipmentId);
    IReadOnlyList<Shipment> GetAll();
}
```

Every method signature uses domain types. The interface could be backed by any system — FREIGHTMASTER, a REST API, a test double — without changing the domain code at all.

### LegacyShipmentGateway — the ACL adapter

```csharp
public sealed class LegacyShipmentGateway(ILegacyFreightClient client, ShipmentTranslator translator)
    : IShipmentGateway
{
    public Shipment CreateShipment(string recipientName, Address destination, Dimensions package)
    {
        var request = translator.ToLegacy(recipientName, destination, package);
        var newId   = client.CreateShipment(request);
        var created = client.FetchShipment(newId)!;
        return translator.ToDomain(created);
    }
}
```

The gateway is the only place where `Legacy.*` types appear alongside domain types. Everything passing through it is translated before it exits.

## Demo Scenarios

```
1. Fetching existing shipments   — 4 pre-seeded Canadian shipments retrieved and displayed using clean domain fields
2. Active shipments only         — Pending and InTransit shipments filtered; Delivered and Failed excluded
3. Delivered check               — IsDelivered() hides the "03" legacy status code behind a bool
4. Booking a new shipment        — domain dimensions (cm, kg) translated to imperial before sending to FREIGHTMASTER
5. Domain model is clean         — no ALL_CAPS fields, no imperial units, no yyyyMMdd strings anywhere in domain layer
```

## When to Use

- Your domain model must integrate with an external or legacy system whose concepts and conventions differ materially from yours — different naming, units, date formats, status codes, or data granularity.
- You want to isolate your domain from a system that is likely to change or be replaced, so that changes to the external system are absorbed by the ACL rather than scattered across domain classes.
- You are migrating away from a legacy system incrementally; the ACL lets new code talk the new language while the old system is still in place.
- Multiple external systems expose the same concept differently (two freight carriers, two payment gateways); each gets its own ACL so the domain model stays unified.

## When NOT to Use

- The external system's model is a near-perfect fit for your domain — a translation layer adds ceremony with no benefit.
- The integration is a single-use script or throwaway tool where clean domain isolation is not worth the extra files.
- You are building a simple CRUD façade over a well-designed REST API that already uses your preferred conventions.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Domain purity | Domain services import nothing from the `Legacy` namespace; they work only with your own types |
| Single place for conversions | All unit conversions, status mappings, and date parsing live in `ShipmentTranslator`; change the legacy format in one class |
| Testability | Domain services can be unit-tested with a stub `IShipmentGateway`; conversion logic is tested independently in `ShipmentTranslator` |
| Replaceability | Swap the legacy system for a REST API or a second carrier by writing a new gateway; `FreightService` is untouched |
| Explicit boundary | The `Legacy` namespace is a clear architectural boundary; code review and dependency analysis can enforce it |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Boilerplate | Every field must be mapped in both directions; a wide legacy schema means a large translator |
| Two models to maintain | When the legacy system adds a new field you care about, you update the legacy DTO, the translator, and the domain model |
| Translation loss | Some legacy concepts may not map cleanly to your domain; the translator must decide how to handle mismatches, which can be non-obvious |

## Related Patterns

- **Repository (4.01)** — the ACL is often implemented as a repository-like gateway: the repository pattern gives it a collection interface, the ACL gives it a translation mandate.
- **Adapter (2.1)** — the ACL gateway class is structurally an Adapter; the difference is intent — Adapter converts interfaces, ACL also converts conceptual models and terminology.
- **DTO (4.23)** — legacy DTOs (`LegacyShipmentRecord`) resemble DTOs, but they represent an external system's schema rather than a deliberate data-transfer contract designed by your team.
- **Facade (2.5)** — like a Facade, the ACL simplifies a complex external system, but a Facade does not translate models — it only aggregates calls.

## Running the Demo

```bash
cd src/4-Enterprise/4.27-AntiCorruptionLayer/AntiCorruptionLayerPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.27-AntiCorruptionLayer/AntiCorruptionLayerPattern.Tests
dotnet test
```
