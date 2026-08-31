using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Options;
using OptionsPattern.Services;

Console.WriteLine("=== Maple Notify — Options Pattern Demo ===\n");

// ── Section 1: IOptions<T> — basic singleton access ──────────────────────────
Console.WriteLine("--- Section 1: IOptions<T> — Basic Singleton Configuration ---");
Console.WriteLine("  IOptions<T> reads config once at startup and never changes.");
Console.WriteLine();

var basicServices = new ServiceCollection();

basicServices.Configure<SmtpOptions>(o =>
{
    o.Host            = "smtp.maple-notify.ca";
    o.Port            = 587;
    o.FromAddress     = "noreply@maple-notify.ca";
    o.FromDisplayName = "Maple Notify";
    o.TimeoutSeconds  = 30;
});

basicServices.Configure<RetryOptions>(o =>
{
    o.MaxAttempts = 3;
    o.BaseDelayMs = 500;
});

basicServices.AddSingleton<EmailDispatcher>();

using var basicProvider = basicServices.BuildServiceProvider();
var dispatcher = basicProvider.GetRequiredService<EmailDispatcher>();
dispatcher.Send(
    to:      "sophie.tremblay@gmail.com",
    subject: "Welcome to Maple Commerce!",
    body:    "Thank you for signing up.");

Pause();

// ── Section 2: Named Options — IOptionsMonitor<T>.Get("name") ─────────────────
Console.WriteLine("--- Section 2: Named Options — Transactional vs Marketing ---");
Console.WriteLine("  Named options let you register multiple configurations of the same type.");
Console.WriteLine();

var namedServices = new ServiceCollection();

namedServices.AddOptions<SmtpOptions>("Transactional")
    .Configure(o =>
    {
        o.Host            = "smtp.maple-notify.ca";
        o.Port            = 587;
        o.FromAddress     = "noreply@maple-notify.ca";
        o.FromDisplayName = "Maple Notify";
        o.TimeoutSeconds  = 30;
    });

namedServices.AddOptions<SmtpOptions>("Marketing")
    .Configure(o =>
    {
        o.Host            = "smtp.maple-notify.ca";
        o.Port            = 587;
        o.FromAddress     = "campaigns@maple-notify.ca";
        o.FromDisplayName = "Maple Campaigns";
        o.TimeoutSeconds  = 60;
    });

namedServices.AddSingleton<NotificationService>();

using var namedProvider = namedServices.BuildServiceProvider();
var notifications = namedProvider.GetRequiredService<NotificationService>();

Console.WriteLine("  Order confirmation (Transactional config):");
notifications.SendOrderConfirmation("marcus.osei@outlook.com", "CA-2026-00192");
Console.WriteLine();
Console.WriteLine("  Newsletter (Marketing config):");
notifications.SendCampaign("alice.tremblay@gmail.com", "Summer Clearance — Up to 60% Off");

Pause();

// ── Section 3: IOptionsMonitor<T> — current value and OnChange subscription ───
Console.WriteLine("--- Section 3: IOptionsMonitor<T> — Watching Configuration ---");
Console.WriteLine("  IOptionsMonitor<T> reads a live value and fires OnChange when");
Console.WriteLine("  the underlying configuration source is reloaded (e.g. appsettings.json).");
Console.WriteLine();

var monitor = namedProvider.GetRequiredService<IOptionsMonitor<SmtpOptions>>();

Console.WriteLine($"  Transactional timeout : {monitor.Get("Transactional").TimeoutSeconds}s");
Console.WriteLine($"  Marketing timeout     : {monitor.Get("Marketing").TimeoutSeconds}s");
Console.WriteLine();

// Subscribing to OnChange — in a hosted app this fires when appsettings.json is saved.
using var subscription = notifications.WatchForChanges((opts, name) =>
    Console.WriteLine($"  [OnChange] '{name ?? "(default)"}' reloaded → timeout now {opts.TimeoutSeconds}s"));

Console.WriteLine("  OnChange subscription registered.");
Console.WriteLine("  (In a hosted .NET app this fires automatically on appsettings.json reload.)");

Pause();

// ── Section 4: Validation — catching invalid configuration at startup ──────────
Console.WriteLine("--- Section 4: ValidateDataAnnotations — Catching Bad Config ---");
Console.WriteLine("  .ValidateDataAnnotations() runs [Required]/[Range]/[EmailAddress] checks");
Console.WriteLine("  and throws OptionsValidationException when .Value is first accessed.");
Console.WriteLine();

var badServices = new ServiceCollection();

badServices.AddOptions<SmtpOptions>()
    .Configure(o =>
    {
        o.Host        = "";               // violates [Required]
        o.Port        = 0;                // violates [Range(1, 65535)]
        o.FromAddress = "not-an-email";   // violates [EmailAddress]
        o.TimeoutSeconds = 4;             // violates [Range(5, 120)]
    })
    .ValidateDataAnnotations();

using var badProvider = badServices.BuildServiceProvider();

try
{
    var _ = badProvider.GetRequiredService<IOptions<SmtpOptions>>().Value;
}
catch (OptionsValidationException ex)
{
    Console.WriteLine("  Caught OptionsValidationException — invalid configuration rejected:");
    foreach (var failure in ex.Failures)
        Console.WriteLine($"    • {failure}");
}

static void Pause()
{
    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine();
}
