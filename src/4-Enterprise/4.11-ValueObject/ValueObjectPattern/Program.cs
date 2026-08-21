using ValueObjectPattern.Domain;
using ValueObjectPattern.Values;

Console.WriteLine("=== Maple Properties — Value Object Demo ===\n");

// ── Section 1: Money — structural equality and immutable arithmetic ───────────
Console.WriteLine("--- Money: Structural Equality ---");

var price1 = new Money(750_000m, "CAD");
var price2 = new Money(750_000m, "cad");   // currency normalized to "CAD"
var price3 = new Money(800_000m, "CAD");

Console.WriteLine($"price1 = {price1}");
Console.WriteLine($"price2 = {price2}  (created with lowercase 'cad')");
Console.WriteLine($"price1 == price2 : {price1 == price2}");  // true — same value
Console.WriteLine($"price1 == price3 : {price1 == price3}");  // false

Console.WriteLine("\n--- Money: Immutable Arithmetic ---");

var askingPrice = new Money(875_000m, "CAD");
var hst         = askingPrice * 0.13m;
var total       = askingPrice + hst;
var deposit     = total * 0.05m;

Console.WriteLine($"Asking:  {askingPrice}");
Console.WriteLine($"HST 13%: {hst}");
Console.WriteLine($"Total:   {total}");
Console.WriteLine($"Deposit: {deposit}");
Console.WriteLine($"askingPrice unchanged: {askingPrice}");   // still $875,000.00

Pause();

// ── Section 2: Address — structural equality and postal code normalization ────
Console.WriteLine("--- Address: Structural Equality & Normalization ---");

var addr1 = new Address("100 Queen St W", "Toronto", "ON", "M5H2N2");
var addr2 = new Address("100 Queen St W", "Toronto", "ON", "m5h 2n2");  // lowercase, no space
var addr3 = new Address("200 Burrard St",  "Vancouver", "BC", "V6C3L6");

Console.WriteLine($"addr1 = {addr1}");
Console.WriteLine($"addr2 = {addr2}  (created with 'm5h 2n2')");
Console.WriteLine($"addr1 == addr2 : {addr1 == addr2}");   // true — normalized to same value
Console.WriteLine($"addr1 == addr3 : {addr1 == addr3}");   // false

Pause();

// ── Section 3: DateRange — contains, overlaps, intersection ──────────────────
Console.WriteLine("--- DateRange: Operations ---");

var listed    = new DateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 11, 30));
var viewing   = new DateRange(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));
var offSeason = new DateRange(new DateOnly(2026, 12, 1), new DateOnly(2027, 2, 28));
var target    = new DateOnly(2026, 10, 15);

Console.WriteLine($"Listed:    {listed}");
Console.WriteLine($"Viewing:   {viewing}");
Console.WriteLine($"OffSeason: {offSeason}");
Console.WriteLine($"Target date ({target:yyyy-MM-dd}) in listed?   {listed.Contains(target)}");
Console.WriteLine($"Viewing overlaps listed?   {viewing.Overlaps(listed)}");
Console.WriteLine($"OffSeason overlaps listed? {offSeason.Overlaps(listed)}");
Console.WriteLine($"Intersection of listed and viewing: {listed.Intersection(viewing)}");

var range1 = new DateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 11, 30));
var range2 = new DateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 11, 30));
Console.WriteLine($"\nrange1 == range2 (same dates): {range1 == range2}");

Pause();

// ── Section 4: PropertyListing using all three value objects ──────────────────
Console.WriteLine("--- PropertyListing: Composing Value Objects ---");

var listing = new PropertyListing(
    id:           1,
    title:        "Downtown Toronto Condo",
    location:     new Address("88 Scott St", "Toronto", "ON", "M5E1A1"),
    askingPrice:  new Money(1_250_000m, "CAD"),
    availability: new DateRange(new DateOnly(2026, 10, 1), new DateOnly(2026, 12, 31)));

Console.WriteLine(listing);

Console.WriteLine("\nReducing price by $50,000 (creates a NEW listing; original unchanged):");
var reduced = listing.WithPrice(listing.AskingPrice - new Money(50_000m, "CAD"));
Console.WriteLine($"  Original: {listing.AskingPrice}");
Console.WriteLine($"  Reduced:  {reduced.AskingPrice}");

Console.WriteLine("\nTwo listings at the same address are interchangeable by address value:");
var listingA = new PropertyListing(10, "Unit A", new Address("88 Scott St", "Toronto", "ON", "M5E1A1"),
                                   new Money(900_000m, "CAD"),
                                   new DateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)));
var listingB = new PropertyListing(11, "Unit B", new Address("88 Scott St", "Toronto", "ON", "m5e1a1"),
                                   new Money(910_000m, "CAD"),
                                   new DateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)));

Console.WriteLine($"  listingA.Location == listingB.Location : {listingA.Location == listingB.Location}");
Console.WriteLine($"  listingA.Availability == listingB.Availability : {listingA.Availability == listingB.Availability}");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
