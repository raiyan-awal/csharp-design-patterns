using PublishSubscribePattern.Core;
using PublishSubscribePattern.Domain;
using PublishSubscribePattern.Events;
using PublishSubscribePattern.Services;

Console.WriteLine("=== 4.24 Publish-Subscribe — Maple News ===");
Console.WriteLine();

// ── Section 1: Basic pub-sub mechanics ──────────────────────────────────────

Console.WriteLine("── 1. Basic Publish-Subscribe ──");
Console.WriteLine();

var basicBus = new InMemoryEventBus();

int handlerACount = 0, handlerBCount = 0;

basicBus.Subscribe<ArticlePublishedEvent>(_ => { handlerACount++; Console.WriteLine("  [Handler A] received ArticlePublishedEvent"); });
basicBus.Subscribe<ArticlePublishedEvent>(_ => { handlerBCount++; Console.WriteLine("  [Handler B] received ArticlePublishedEvent"); });

var demoArticle = new Article(Guid.NewGuid(), "TSX Hits Record High", "Markets rallied...", ArticleCategory.Business, "Maya Okonkwo", DateTimeOffset.UtcNow);
basicBus.Publish(new ArticlePublishedEvent(demoArticle, DateTimeOffset.UtcNow));

Console.WriteLine($"  Handler A called: {handlerACount}x, Handler B called: {handlerBCount}x");

Pause();

// ── Section 2: Unsubscribe ───────────────────────────────────────────────────

Console.WriteLine("── 2. Unsubscribe ──");
Console.WriteLine();

void TransientHandler(ArticlePublishedEvent e) =>
    Console.WriteLine($"  [Transient] received '{e.Article.Title}'");

basicBus.Subscribe<ArticlePublishedEvent>(TransientHandler);
Console.WriteLine("  Transient handler subscribed — publishing...");
basicBus.Publish(new ArticlePublishedEvent(demoArticle, DateTimeOffset.UtcNow));

basicBus.Unsubscribe<ArticlePublishedEvent>(TransientHandler);
Console.WriteLine("  Transient handler unsubscribed — publishing...");
basicBus.Publish(new ArticlePublishedEvent(demoArticle, DateTimeOffset.UtcNow));
Console.WriteLine("  (Transient handler was NOT called on second publish)");

Pause();

// ── Section 3: Type filtering ─────────────────────────────────────────────────

Console.WriteLine("── 3. Type Filtering ──");
Console.WriteLine();

var filterBus = new InMemoryEventBus();

filterBus.Subscribe<ArticlePublishedEvent>(e =>
    Console.WriteLine($"  [Subscriber A] only cares about ArticlePublishedEvent — got '{e.Article.Title}'"));

filterBus.Subscribe<BreakingNewsAlertEvent>(e =>
    Console.WriteLine($"  [Subscriber B] only cares about BreakingNewsAlertEvent — got: {e.AlertHeadline}"));

Console.WriteLine("  Publishing ArticlePublishedEvent (only Subscriber A should fire):");
filterBus.Publish(new ArticlePublishedEvent(demoArticle, DateTimeOffset.UtcNow));

Console.WriteLine("  Publishing BreakingNewsAlertEvent (only Subscriber B should fire):");
filterBus.Publish(new BreakingNewsAlertEvent(demoArticle, "BREAKING: TSX hits record", DateTimeOffset.UtcNow));

Pause();

// ── Section 4: Full Maple News newsroom ──────────────────────────────────────

Console.WriteLine("── 4. Maple News Newsroom — All Services Wired ──");
Console.WriteLine();

var bus = new InMemoryEventBus();

var editorial  = new EditorialService(bus);
var digest     = new EmailDigestService();
var alerts     = new BreakingNewsAlertService();
var analytics  = new AnalyticsService();
var archive    = new ContentArchiveService();

bus.Subscribe<ArticlePublishedEvent>(digest.OnArticlePublished);
bus.Subscribe<ArticlePublishedEvent>(analytics.OnArticlePublished);
bus.Subscribe<ArticlePublishedEvent>(archive.OnArticlePublished);
bus.Subscribe<BreakingNewsAlertEvent>(alerts.OnBreakingNewsAlert);
bus.Subscribe<ArticleUpdatedEvent>(analytics.OnArticleUpdated);
bus.Subscribe<ArticleUpdatedEvent>(archive.OnArticleUpdated);

Console.WriteLine("  Publishing: Tech article...");
var techArticle = editorial.PublishArticle(
    "Ottawa Unveils $2B AI Research Fund",
    "The federal government announced a $2 billion investment in AI research centres across Toronto, Montreal, and Vancouver.",
    ArticleCategory.Technology,
    "Priya Sharma");

Console.WriteLine();
Console.WriteLine("  Publishing: Sports article...");
editorial.PublishArticle(
    "Toronto Raptors Advance to NBA Playoffs",
    "The Raptors secured their playoff spot with a decisive win at Scotiabank Arena on Saturday night.",
    ArticleCategory.Sports,
    "James Beaumont");

Pause();

// ── Section 5: Breaking news alert ───────────────────────────────────────────

Console.WriteLine("── 5. Breaking News Alert ──");
Console.WriteLine();

Console.WriteLine("  Publishing: Breaking news (triggers both ArticlePublished and BreakingNewsAlert)...");
editorial.PublishArticle(
    "Bank of Canada Cuts Rate to 2.25%",
    "Governor Macklem announced an emergency quarter-point cut effective immediately.",
    ArticleCategory.BreakingNews,
    "Claire Tremblay");

Console.WriteLine();
Console.WriteLine($"  Total alerts sent: {alerts.AlertsSent.Count}");
foreach (var alert in alerts.AlertsSent)
    Console.WriteLine($"    • {alert}");

Pause();

// ── Section 6: Article correction ─────────────────────────────────────────────

Console.WriteLine("── 6. Article Correction / Update ──");
Console.WriteLine();

Console.WriteLine("  Correcting the AI Fund article (archive + analytics notified)...");
editorial.UpdateArticle(
    techArticle.Id,
    "The federal government announced a $2 billion investment in AI research centres across Toronto, Montreal, and Edmonton.",
    "Correction: Edmonton, not Vancouver");

Pause();

// ── Section 7: Email digest flush ─────────────────────────────────────────────

Console.WriteLine("── 7. Email Digest Flush ──");
Console.WriteLine();

var batch = digest.FlushDigest();
Console.WriteLine($"  Flushing digest — {batch.Count} article(s) queued:");
foreach (var a in batch)
    Console.WriteLine($"    • [{a.Category}] {a.Title} — by {a.Author}");

Console.WriteLine($"  Pending after flush: {digest.Pending.Count}");

Pause();

// ── Section 8: Analytics summary ──────────────────────────────────────────────

Console.WriteLine("── 8. Analytics Summary ──");
Console.WriteLine();
analytics.PrintSummary();

Console.WriteLine();
Console.WriteLine($"  Archive size: {archive.Archive.Count} article(s), {archive.Log.Count} log entries");
Console.WriteLine();
Console.WriteLine("=== End of Demo ===");

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
