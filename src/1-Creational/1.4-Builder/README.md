# 1.4 — Builder

## Intent

Separate the construction of a complex object from its representation so that the same construction process can produce different representations, and so that optional parts can be omitted cleanly without combinatorial constructor overloads.

## The Problem It Solves

As an object accumulates optional fields, constructors multiply into the "telescoping constructor" anti-pattern:

```csharp
// Without Builder: one constructor per combination of optional fields
public Email(string to, string subject, string body) { ... }
public Email(string to, string from, string subject, string body) { ... }
public Email(string to, string from, string subject, string body, bool isHtml) { ... }
public Email(string to, string from, string subject, string body, bool isHtml,
             string replyTo, List<string> cc, List<string> bcc, int priority) { ... }
// 9 fields, dozens of meaningful combinations — constructor count explodes
```

Problems with this approach:
- Callers must pass `null` or default values for fields they do not need.
- Adding a new optional field requires adding another overload or changing all existing ones.
- At the call site, positional arguments like `new Email(to, null, subject, body, false, null, null, null, 1)` are impossible to read.
- Validation that spans multiple fields (e.g., HTML body requires content-type header) is scattered or duplicated.

## Solution: Step-by-step construction with Build() validation

`EmailBuilder` exposes one method per field, all returning `this` for fluent chaining. `Build()` runs cross-field validation once and produces an immutable `Email`. `EmailDirector` packages common multi-step sequences into named templates.

```csharp
// Fluent builder — only set what you need, readable at a glance:
var email = new EmailBuilder()
    .To("client@example.ca")
    .WithSubject("Your order has shipped")
    .WithBody("<p>Your package is on its way!</p>")
    .AsHtml()
    .WithPriority(EmailPriority.High)
    .Build();

// Director pre-packages a full welcome-email template in one call:
var welcome = director.BuildWelcomeEmail("new.member@example.ca", "Alice");
```

## Participants

| Role | Class | Responsibility |
|------|-------|----------------|
| Product | `Email` | Immutable sealed class with `internal` constructor; fields include required (`To`, `Subject`, `Body`) and optional (`CcRecipients`, `BccRecipients`, `Attachments`, `IsHtml`, `Priority`, `ReplyTo`) |
| Builder interface | `IEmailBuilder` | Contract declaring all step methods and `Build()`; `Reset()` returns `IEmailBuilder` for chaining |
| Concrete builder | `EmailBuilder` | Accumulates field values in mutable state; `Build()` validates required fields and returns `Email`; state persists after `Build()` — call `Reset()` explicitly to reuse |
| Priority enum | `EmailPriority` | `Low`, `Normal`, `High` — passed to `WithPriority(EmailPriority)` |
| Director | `EmailDirector` | Encapsulates four named templates (`BuildWelcomeEmail`, `BuildPasswordResetEmail`, `BuildNewsletterEmail`, `BuildInvoiceEmail`); calls `Reset()` at the start of each template |

## Structure

```
1.4-Builder/
├── BuilderPattern/
│   ├── Email.cs           ← immutable product with internal constructor
│   ├── IEmailBuilder.cs   ← builder interface
│   ├── EmailBuilder.cs    ← concrete builder with fluent API and Build() validation
│   ├── EmailDirector.cs   ← four pre-packaged email templates
│   └── Program.cs
└── BuilderPattern.Tests/
    └── BuilderPatternTests.cs
```

## Key Code

### Immutable product with internal constructor

```csharp
public sealed class Email
{
    public string   To       { get; }
    public string   Subject  { get; }
    public string   Body     { get; }
    public bool     IsHtml   { get; }
    public string?  From     { get; }
    public string?  ReplyTo  { get; }
    public IReadOnlyList<string> Cc  { get; }
    public IReadOnlyList<string> Bcc { get; }
    public int Priority { get; }

    // internal — only EmailBuilder can call this
    internal Email(string to, string subject, string body, ...) { ... }
}
```

The `internal` constructor means `new Email(...)` is unavailable outside the assembly. The only way to obtain an `Email` is through `EmailBuilder.Build()`.

### Builder with validation in Build()

```csharp
public sealed class EmailBuilder : IEmailBuilder
{
    private string?        _to, _subject, _body, _replyTo;
    private bool           _isHtml   = false;
    private EmailPriority  _priority = EmailPriority.Normal;
    private readonly List<string> _cc = [], _bcc = [], _attachments = [];

    public IEmailBuilder To(string recipient)       { _to = recipient;   return this; }
    public IEmailBuilder WithSubject(string subject){ _subject = subject; return this; }
    public IEmailBuilder WithBody(string body)      { _body = body;       return this; }
    public IEmailBuilder AsHtml()                   { _isHtml = true;     return this; }
    public IEmailBuilder WithPriority(EmailPriority p) { _priority = p;  return this; }
    public IEmailBuilder Cc(string r)    { _cc.Add(r);          return this; }
    public IEmailBuilder Attach(string f){ _attachments.Add(f); return this; }

    public Email Build()
    {
        if (string.IsNullOrWhiteSpace(_to))      throw new InvalidOperationException("To is required.");
        if (string.IsNullOrWhiteSpace(_subject)) throw new InvalidOperationException("Subject is required.");
        if (_body is null)                        throw new InvalidOperationException("Body is required.");

        // State is NOT reset here — call Reset() explicitly to reuse the builder
        return new Email(_to!, _subject!, _body!, _cc.AsReadOnly(), _bcc.AsReadOnly(),
                         _attachments.AsReadOnly(), _isHtml, _priority, _replyTo);
    }

    public IEmailBuilder Reset()
    {
        _to = _subject = _body = _replyTo = null;
        _isHtml = false; _priority = EmailPriority.Normal;
        _cc.Clear(); _bcc.Clear(); _attachments.Clear();
        return this;
    }
}
```

`Build()` does not reset state automatically. The `EmailDirector` calls `Reset()` at the start of each template recipe rather than relying on the previous call having cleaned up.

### Director — reusable named templates

```csharp
public sealed class EmailDirector(IEmailBuilder builder)
{
    // Reset() is called first so the director's use is idempotent
    public Email BuildWelcomeEmail(string to, string firstName) =>
        builder.Reset()
            .To(to)
            .WithSubject($"Welcome to our platform, {firstName}!")
            .WithBody($"Hi {firstName}, your account is ready.")
            .WithPriority(EmailPriority.Normal)
            .Build();

    public Email BuildPasswordResetEmail(string to, string resetLink) =>
        builder.Reset()
            .To(to)
            .WithSubject("Password reset request")
            .WithBody($"Click to reset: {resetLink}")
            .WithPriority(EmailPriority.High)
            .WithReplyTo("no-reply@example.com")
            .Build();

    // BuildNewsletterEmail(to, htmlContent) — Low priority, HTML, Bcc unsubscribe
    // BuildInvoiceEmail(to, customerName, pdfPath) — HTML, Attach PDF, Cc accounting
}
```

The director is optional — callers can drive the builder directly when no standard template fits.

## Demo Scenarios

```
1. Basic fluent construction  — build a minimal email with only the required fields
2. Complex email              — chain all optional fields (CC, BCC, attachments, reply-to, priority)
3. Director templates         — BuildWelcomeEmail, BuildPasswordResetEmail, BuildNewsletterEmail,
                                BuildInvoiceEmail called with one method each
4. Validation demo            — Build() throws when required fields are missing
5. Builder reuse after Reset  — same builder instance produces two different emails sequentially
```

## When to Use

- Objects have many optional parameters and you want to avoid positional `null` arguments.
- Construction involves validation that spans multiple fields and should run once, at `Build()`.
- You want to provide named "presets" or templates for common configurations (via a Director).
- The object should be immutable once created, but its construction process is inherently mutable.

## When NOT to Use

- When the object has only two or three fields — a simple constructor is clearer.
- When all fields are required — every Builder method must be called anyway, adding boilerplate with no benefit.
- When construction is trivial and no validation is needed — the pattern adds a builder class for nothing.

## Benefits

| Benefit | Explanation |
|---------|-------------|
| Readable call sites | `builder.To(...).Subject(...).HtmlBody(...).Build()` names every field at the call site |
| Centralized validation | All cross-field checks live in `Build()`; no duplicated guards at each call site |
| Immutable product | `Email` is sealed and read-only once constructed; no risk of post-construction mutation |
| Reusable builder | `Reset()` clears state so the same builder instance constructs multiple objects |

## Drawbacks

| Drawback | Explanation |
|----------|-------------|
| Extra classes | Builder + optional Director add two classes per product type |
| Runtime not compile-time errors | A missing required field is caught at `Build()`, not by the compiler |
| Verbosity for simple objects | If an object has two fields, a constructor is always clearer |

## Related Patterns

- **Abstract Factory (1.3)** — Abstract Factory creates an object in one call; Builder constructs it step by step, which is better when construction requires many optional decisions.
- **Template Method (3.10)** — Directors often use a Template Method to define a fixed construction sequence with overridable steps.
- **Fluent Interface** — Builder is the canonical example of a fluent API; each method returns `this` to enable chaining.
- **Value Object (4.11)** — Builder is the natural construction path for complex Value Objects that need validation before becoming immutable.

## Running the Demo

```bash
cd src/1-Creational/1.4-Builder/BuilderPattern
dotnet run
```

## Running the Tests

```bash
cd src/1-Creational/1.4-Builder/BuilderPattern.Tests
dotnet test
```
