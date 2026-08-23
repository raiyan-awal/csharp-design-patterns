using DomainEventPattern.Domain;
using DomainEventPattern.Handlers;
using DomainEventPattern.Infrastructure;

Console.WriteLine("=== Maple Auctions — Domain Event Demo ===\n");

// ── Wire up the dispatcher ─────────────────────────────────────────────────────
var dispatcher  = new DomainEventDispatcher();
var audit       = new AuditLogHandler();
var email       = new EmailNotificationHandler();
var fraud       = new FraudDetectionHandler();

dispatcher.Register<DomainEventPattern.Events.AuctionOpenedEvent>(audit);
dispatcher.Register<DomainEventPattern.Events.BidPlacedEvent>(audit);
dispatcher.Register<DomainEventPattern.Events.AuctionClosedEvent>(audit);

dispatcher.Register<DomainEventPattern.Events.BidPlacedEvent>(email);
dispatcher.Register<DomainEventPattern.Events.AuctionClosedEvent>(email);

dispatcher.Register<DomainEventPattern.Events.BidPlacedEvent>(fraud);

// ── Section 1: Open an auction ────────────────────────────────────────────────
Console.WriteLine("--- Opening Auctions ---");

var groupOfSeven = new Auction(1, "Group of Seven Landscape — Lawren Harris (1924)", reservePrice: 850_000m);
var inuit        = new Auction(2, "Inuit Soapstone Sculpture — Kenojuak Ashevak",    reservePrice: 12_000m);

dispatcher.DispatchAndClear(groupOfSeven);
dispatcher.DispatchAndClear(inuit);

Pause();

// ── Section 2: Place bids ─────────────────────────────────────────────────────
Console.WriteLine("--- Bidding on Group of Seven Landscape ---");

groupOfSeven.PlaceBid("Laurent Beauchamp", 860_000m);
groupOfSeven.PlaceBid("Priya Nair",        890_000m);
groupOfSeven.PlaceBid("Laurent Beauchamp", 920_000m);
groupOfSeven.PlaceBid("Lauren Beauchamp",  950_000m);  // different person — similar name

dispatcher.DispatchAndClear(groupOfSeven);

Pause();

// ── Section 3: Shill bid detection ───────────────────────────────────────────
Console.WriteLine("--- Bidding on Inuit Sculpture (shill bid scenario) ---");

inuit.PlaceBid("Marcus Holt",  13_000m);
inuit.PlaceBid("Sandra Chu",   14_500m);
inuit.PlaceBid("Sandra Chu",   15_000m);  // same winner raising own bid — fraud alert

dispatcher.DispatchAndClear(inuit);

Pause();

// ── Section 4: Close auctions ─────────────────────────────────────────────────
Console.WriteLine("--- Closing Auctions ---");

groupOfSeven.Close();
inuit.Close();

dispatcher.DispatchAndClear(groupOfSeven);
dispatcher.DispatchAndClear(inuit);

Pause();

// ── Section 5: Auction with reserve not met ───────────────────────────────────
Console.WriteLine("--- Auction Where Reserve Is Not Met ---");

var reserve = new Auction(3, "Contemporary Ottawa Sculpture — Kenojuak Ashevak (unsigned)", reservePrice: 50_000m);
dispatcher.DispatchAndClear(reserve);

reserve.PlaceBid("James Tremblay", 35_000m);
reserve.PlaceBid("Aisha Okafor",   42_000m);
dispatcher.DispatchAndClear(reserve);

reserve.Close();
dispatcher.DispatchAndClear(reserve);

Console.WriteLine($"\nFraud alerts raised this session: {fraud.Alerts.Count}");
Console.WriteLine($"Total audit entries: {audit.Log.Count}");
Console.WriteLine($"Total emails sent: {email.SentEmails.Count}");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
