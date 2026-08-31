# 4.22 — Options Pattern

## Intent

The Options Pattern provides strongly-typed access to groups of related configuration settings. Instead of reading raw strings from `IConfiguration` with magic keys like `config["Smtp:Host"]`, you define a plain C# class whose properties map to your configuration section, register it with the DI container, and inject it wherever you need the settings. The pattern also supports named configurations (multiple independent instances of the same options class), runtime validation, and live reload notifications.

## The Problem It Solves

Consider reading SMTP settings the raw way:

```csharp
// Without Options Pattern
public EmailSender(IConfiguration config)
{
    var host    = config["Smtp:Host"];           // string? — typo in key = silent null
    var port    = int.Parse(config["Smtp:Port"]!); // throws if key missing
    var timeout = int.Parse(config["Smtp:TimeoutSeconds"]!);
    // ...
}
```

Problems:
- Configuration keys are magic strings — a typo silently returns null instead of a compile error.
- No type safety — every value comes back as `string?` and must be parsed manually.
- No validation — an empty host or a negative port is not caught until the email actually fails to send.
- Scattered reads — the same key might be parsed in five different places with five different defaults.

## Solution: Bind Configuration to a Strongly-Typed Class

```csharp
// Register once
services.AddOptions<SmtpOptions>()
    .Configure(o => { o.Host = "smtp.maple-notify.ca"; o.Port = 587; })
    .ValidateDataAnnotations();

// Inject and use
public EmailDispatcher(IOptions<SmtpOptions> smtpOptions)
{
    _smtp = smtpOptions.Value;  // SmtpOptions — fully typed, validated
}
```

The compiler catches property name typos. Validation runs at startup. All consumers share the same configured object.

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Options class | `SmtpOptions` | Holds SMTP configuration with data annotation constraints |
| Options class | `RetryOptions` | Holds retry policy settings with range constraints |
| Singleton consumer | `EmailDispatcher` | Reads the default `IOptions<SmtpOptions>` once at startup |
| Monitor consumer | `NotificationService` | Reads named configs via `IOptionsMonitor<SmtpOptions>` |

## Structure

```
src/4-Enterprise/4.22-OptionsPattern/
├── OptionsPattern/
│   ├── Options/
│   │   ├── SmtpOptions.cs          ← [Required], [Range], [EmailAddress] attributes
│   │   └── RetryOptions.cs         ← [Range] on MaxAttempts and BaseDelayMs
│   ├── Services/
│   │   ├── EmailDispatcher.cs      ← IOptions<T> — singleton, read once
│   │   └── NotificationService.cs  ← IOptionsMonitor<T> — named configs + OnChange
│   └── Program.cs
└── OptionsPattern.Tests/
    └── OptionsPatternTests.cs      ← 30 tests across 6 suites
```

## Key Code

### Strongly-typed options with validation attributes

```csharp
public sealed class SmtpOptions
{
    [Required(AllowEmptyStrings = false)]
    public string Host { get; set; } = "";

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public string FromAddress { get; set; } = "";

    [Range(5, 120)]
    public int TimeoutSeconds { get; set; } = 30;
}
```

The data annotation attributes are the contract for valid configuration. When `.ValidateDataAnnotations()` is added to the registration, these constraints are enforced the first time `.Value` is accessed — before any email is ever sent.

### IOptions\<T\> — singleton, read once at startup

```csharp
public sealed class EmailDispatcher(IOptions<SmtpOptions> smtpOptions, IOptions<RetryOptions> retryOptions)
{
    public SmtpOptions  SmtpConfig  { get; } = smtpOptions.Value;
    public RetryOptions RetryConfig { get; } = retryOptions.Value;
}
```

`IOptions<T>` is a singleton. `.Value` is evaluated once when the property is first accessed and never changes for the lifetime of the application. Use it when your settings are stable and come from a fixed source (environment variables, hardcoded configuration, a one-time file read).

### Named options — multiple configurations of the same type

```csharp
services.AddOptions<SmtpOptions>("Transactional")
    .Configure(o => { o.FromAddress = "noreply@maple-notify.ca"; o.TimeoutSeconds = 30; });

services.AddOptions<SmtpOptions>("Marketing")
    .Configure(o => { o.FromAddress = "campaigns@maple-notify.ca"; o.TimeoutSeconds = 60; });
```

```csharp
public sealed class NotificationService(IOptionsMonitor<SmtpOptions> smtpMonitor)
{
    public void SendOrderConfirmation(string customerEmail, string orderRef)
    {
        var smtp = smtpMonitor.Get("Transactional");   // picks the "Transactional" instance
        // ...
    }

    public void SendCampaign(string customerEmail, string campaignName)
    {
        var smtp = smtpMonitor.Get("Marketing");        // picks the "Marketing" instance
        // ...
    }
}
```

Named options let you register two completely independent `SmtpOptions` objects under different names and retrieve each by name at runtime. The `"Transactional"` instance uses a `noreply@` address with a 30-second timeout; `"Marketing"` uses a `campaigns@` address with a 60-second timeout. Both coexist in the same container.

### IOptionsMonitor\<T\> — live reload and OnChange

```csharp
using var subscription = smtpMonitor.OnChange((opts, name) =>
    Console.WriteLine($"Config '{name}' reloaded — timeout now {opts.TimeoutSeconds}s"));
```

`IOptionsMonitor<T>` is a singleton like `IOptions<T>`, but its `CurrentValue` property is re-evaluated every time the underlying configuration source signals a change (for example, when `appsettings.json` is saved on disk). The `OnChange` callback fires on every reload, giving you a hook to flush caches, log changes, or update dependent state. Dispose the returned `IDisposable` to unsubscribe.

### ValidateDataAnnotations — catching bad config at startup

```csharp
services.AddOptions<SmtpOptions>()
    .Configure(o => { o.Host = ""; o.Port = 0; o.FromAddress = "not-an-email"; })
    .ValidateDataAnnotations();

// Later, accessing .Value throws:
var opts = provider.GetRequiredService<IOptions<SmtpOptions>>().Value;
// OptionsValidationException: DataAnnotation validation failed for 'SmtpOptions'
//   • The Host field is required.
//   • The field Port must be between 1 and 65535.
//   • The FromAddress field is not a valid e-mail address.
```

`.ValidateDataAnnotations()` wires up a validator that runs `Validator.TryValidateObject` against the configured instance the first time `.Value` is accessed. Combined with `.ValidateOnStart()` (requires `IHost`), validation runs immediately on application startup — before any request is served — so misconfigured environments fail loudly rather than silently.

## Demo Scenarios

```
=== Maple Notify — Options Pattern Demo ===

--- Section 1: IOptions<T> — Basic Singleton Configuration ---
  Sending via smtp.maple-notify.ca:587
  From    : Maple Notify <noreply@maple-notify.ca>
  To      : sophie.tremblay@gmail.com
  Subject : Welcome to Maple Commerce!
  Timeout : 30s  |  Retry: up to 3 attempts (base 500ms delay)

--- Section 2: Named Options — Transactional vs Marketing ---
  [Maple Notify] noreply@maple-notify.ca → marcus.osei@outlook.com
  Subject : Your Maple order CA-2026-00192 is confirmed

  [Maple Campaigns] campaigns@maple-notify.ca → alice.tremblay@gmail.com
  Campaign: Summer Clearance — Up to 60% Off

--- Section 3: IOptionsMonitor<T> — Watching Configuration ---
  Transactional timeout : 30s
  Marketing timeout     : 60s
  OnChange subscription registered.
  (In a hosted .NET app this fires automatically on appsettings.json reload.)

--- Section 4: ValidateDataAnnotations — Catching Bad Config ---
  Caught OptionsValidationException — invalid configuration rejected:
    • The Host field is required.
    • The field Port must be between 1 and 65535.
    • The FromAddress field is not a valid e-mail address.
    • The field TimeoutSeconds must be between 5 and 120.
```

## When to Use

- You have groups of related settings that belong together (SMTP, JWT, rate-limit parameters) and want to pass them around as a typed unit rather than individual strings.
- You want compile-time safety for configuration key names and types.
- You need multiple independent configurations of the same shape (named options for "Transactional" vs "Marketing" email, or multiple database connection profiles).
- You want settings to reload at runtime without restarting the application (live `appsettings.json` changes with `IOptionsMonitor<T>`).
- You want to validate configuration at startup and fail fast if the deployment is misconfigured.

## When NOT to Use

- Simple single-value settings that do not group naturally — a single feature flag or a connection string by itself rarely warrants a full options class.
- Configuration that changes so frequently (per-request) that the singleton behaviour of `IOptions<T>` is a problem — consider `IOptionsSnapshot<T>` (scoped, re-evaluated per DI scope) in those cases.
- Contexts without a DI container — the pattern depends on `ServiceCollection` wiring; raw `IConfiguration` access is simpler for console scripts or utilities.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Type safety | Property names are C# identifiers — typos are compile errors, not silent nulls. |
| Validation | Data annotation constraints are enforced before any code uses the settings. |
| Named instances | Multiple independent configurations of the same type coexist in one container. |
| Live reload | `IOptionsMonitor<T>` picks up `appsettings.json` changes without restarting. |
| Testability | Pass `new OptionsWrapper<T>(...)` in tests — no DI container, no appsettings file needed. |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Boilerplate | Each settings group needs a class, a `Section` constant, and DI registration. |
| Singleton limitation | `IOptions<T>` never reloads. If you need per-scope or per-request freshness, use `IOptionsSnapshot<T>` — but it requires a scoped DI lifetime. |
| Validation timing | Without `.ValidateOnStart()` (requires `IHost`), validation only runs when `.Value` is first accessed, which may be late in the application lifecycle. |

## Related Patterns

- **Dependency Injection (4.05)** — the Options Pattern is built on DI; `IOptions<T>` is just a registered singleton resolved from the container.
- **Service Layer (4.06)** — service classes commonly inject `IOptions<T>` for their operational settings (timeouts, limits, connection strings).
- **Result Pattern (4.21)** — validation failures in options are surfaced as exceptions; a Result-returning validation layer could wrap `OptionsValidationException` for uniform error handling across the startup pipeline.

## Running the Demo

```bash
cd src/4-Enterprise/4.22-OptionsPattern/OptionsPattern
dotnet run
```

## Running the Tests

```bash
cd src/4-Enterprise/4.22-OptionsPattern/OptionsPattern.Tests
dotnet test
```
