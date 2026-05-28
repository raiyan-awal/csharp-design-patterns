# Builder Pattern

## 📖 Pattern Category
**Creational Pattern**

## 🎯 Intent
Separate the **construction** of a complex object from its **representation**, so the same construction process can create different representations — and so you never end up with a partially-built object in the wild.

## 🤔 Problem
You're building an `Email` class. A fully-featured email has two required fields (recipient, subject, body) and six optional ones (CC list, BCC list, attachments, HTML flag, priority, reply-to).

The obvious approach — a constructor — falls apart:

```csharp
// Which is CC and which is BCC? What if you don't need BCC?
var email = new Email(
    "to@example.com", "Subject", "Body",
    new[] { "cc@example.com" }, Array.Empty<string>(),
    new[] { "file.pdf" }, true, EmailPriority.High, null);
```

This is called the **Telescoping Constructor anti-pattern**: as optional fields multiply, you get constructor overloads or a single massive constructor where positional arguments lose all meaning, and nothing stops you passing a BCC address as the CC argument.

## ✅ Solution

The Builder pattern fixes this by:

1. **Removing the public constructor** from the product — only the builder can create it
2. **Giving each field its own named method** — `.Cc("addr")` cannot be confused with `.Bcc("addr")`
3. **Accumulating state** in the builder until you explicitly call `Build()`
4. **Validating in `Build()`** — required fields are checked before the product object is created
5. **Providing a Director** (optional) that encodes named construction recipes for common shapes

## 🏗️ Structure

```
         «interface»
         IEmailBuilder
    ┌──────────────────────────┐
    │ To(recipient)            │
    │ WithSubject(subject)     │
    │ WithBody(body)           │
    │ Cc(recipient)            │  ← each method returns IEmailBuilder
    │ Bcc(recipient)           │    (enables method chaining)
    │ Attach(filePath)         │
    │ AsHtml()                 │
    │ WithPriority(priority)   │
    │ WithReplyTo(replyTo)     │
    │ Build() → Email          │  ← terminal step; validates + constructs
    │ Reset() → IEmailBuilder  │  ← clears state for reuse
    └──────────────────────────┘
              ▲
              │ implements
         EmailBuilder                     EmailDirector
    ┌──────────────────┐             ┌──────────────────────────────┐
    │ _to              │             │ - _builder: IEmailBuilder    │
    │ _subject         │◄────────────│ BuildWelcomeEmail(...)       │
    │ _body            │             │ BuildPasswordResetEmail(...) │
    │ _cc: List<string>│             │ BuildNewsletterEmail(...)    │
    │ _bcc             │             │ BuildInvoiceEmail(...)       │
    │ _attachments     │             └──────────────────────────────┘
    │ Build() → Email  │                   (uses IEmailBuilder only)
    └──────────────────┘
              │ creates
              ▼
           Email  (the PRODUCT — immutable, internal constructor)
    ┌───────────────────────┐
    │ To: string            │
    │ Subject: string       │
    │ Body: string          │
    │ CcRecipients          │
    │ BccRecipients         │
    │ Attachments           │
    │ IsHtml: bool          │
    │ Priority              │
    │ ReplyTo: string?      │
    └───────────────────────┘
```

## 💻 Implementation in This Example

### Files:
- **Email.cs** — Product (`Email` + `EmailPriority` enum); `internal` constructor keeps it builder-only
- **IEmailBuilder.cs** — Abstract Builder interface; all methods return `IEmailBuilder` for chaining
- **EmailBuilder.cs** — Concrete Builder; accumulates state, validates in `Build()`, supports `Reset()`
- **EmailDirector.cs** — Director; four template methods (Welcome, PasswordReset, Newsletter, Invoice)
- **Program.cs** — Demo with 5 demonstrations + Pause() between each

### Key Implementation Points:

**1. Abstract Builder interface** — each step returns the interface for chaining:
```csharp
public interface IEmailBuilder
{
    IEmailBuilder To(string recipient);
    IEmailBuilder WithSubject(string subject);
    IEmailBuilder WithBody(string body);
    IEmailBuilder Cc(string recipient);        // additive — call multiple times
    IEmailBuilder Attach(string filePath);     // additive — call multiple times
    IEmailBuilder AsHtml();
    IEmailBuilder WithPriority(EmailPriority priority);
    Email Build();    // validates + constructs
    IEmailBuilder Reset();
}
```

**2. Concrete Builder** — accumulates state, validates required fields at Build():
```csharp
public sealed class EmailBuilder : IEmailBuilder
{
    private string? _to, _subject, _body;
    private readonly List<string> _cc = [], _bcc = [], _attachments = [];
    private bool _isHtml = false;
    private EmailPriority _priority = EmailPriority.Normal;

    public IEmailBuilder To(string recipient) { _to = recipient; return this; }

    public Email Build()
    {
        if (string.IsNullOrWhiteSpace(_to))
            throw new InvalidOperationException("To is required.");
        // ... other guards ...
        return new Email(_to, _subject!, _body!, _cc.AsReadOnly(), ...);
    }
}
```

**3. Product** — immutable, internal constructor:
```csharp
public sealed class Email
{
    public string To      { get; }
    public string Subject { get; }
    // ...

    internal Email(string to, string subject, ...)  // only EmailBuilder can call this
    {
        To = to; Subject = subject; // ...
    }
}
```

**4. Director** — recipes for common templates (optional layer):
```csharp
public sealed class EmailDirector(IEmailBuilder builder)
{
    public Email BuildPasswordResetEmail(string to, string resetLink) =>
        builder
            .Reset()
            .To(to)
            .WithSubject("Password reset request")
            .WithBody($"Click: {resetLink}")
            .WithPriority(EmailPriority.High)
            .WithReplyTo("no-reply@example.com")
            .Build();
}
```

**5. Fluent usage** — caller only touches the builder interface:
```csharp
Email email = new EmailBuilder()
    .To("bob@example.com")
    .WithSubject("Q3 Report — Confidential")
    .WithBody("<h1>Q3 Report</h1><p>See attachment.</p>")
    .AsHtml()
    .WithPriority(EmailPriority.High)
    .Cc("cfo@example.com")
    .Attach("/reports/Q3.pdf")
    .Build();
```

## 🔍 Builder vs Competing Approaches

| | Telescoping Constructor | Object Initializer | Builder |
|---|---|---|---|
| **Required fields enforced** | ✗ (silent nulls) | ✗ (silent nulls) | ✅ (Build() throws) |
| **Readability** | ✗ (positional args) | ✅ (named) | ✅ (named methods) |
| **Optional fields** | Messy (overloads or nulls) | ✅ | ✅ |
| **Immutable product** | ✅ | ✗ (setters stay public) | ✅ |
| **Director / recipes** | ✗ | ✗ | ✅ |
| **Validation before creation** | ✗ | ✗ | ✅ |

> **Object initialisers** (`new Email { To = "...", Subject = "..." }`) look similar but they require public setters on the product — meaning anyone can mutate it after construction. A Builder can keep the product constructor `internal` and the properties get-only.

## 🚀 How to Run

```bash
cd src/1-Creational/1.4-Builder/BuilderPattern
dotnet run
```

## 🧪 Running Tests

```bash
cd src/1-Creational/1.4-Builder/BuilderPattern.Tests
dotnet test
```

## 🧪 What the Demo Shows

1. **Basic fluent builder** — three required fields, all optional fields at defaults
2. **Complex email** — CC, BCC, multiple attachments, HTML, High priority, ReplyTo
3. **Director** — four named templates (Welcome, PasswordReset, Newsletter, Invoice)
4. **Why Builder?** — side-by-side comparison with the constructor approach
5. **Validation + reuse** — Build() guards against missing required fields; Reset() reuses the builder

## ✅ Benefits

| Benefit | Description |
|---------|-------------|
| **Prevents invalid objects** | Required fields are enforced in `Build()` — a half-built object can never escape |
| **Self-documenting code** | `.Cc("addr")` is impossible to confuse with `.Bcc("addr")` |
| **Handles optional fields cleanly** | Skip what you don't need — no null-passing ceremony |
| **Immutable product** | Once built, the Email cannot change |
| **Director enables reuse** | Common shapes have names and are tested in one place |
| **Single Responsibility** | Construction logic lives in the builder, not the product |

## ❌ Drawbacks

| Drawback | Description |
|----------|-------------|
| **More classes** | You add a builder (and optionally a director) per product |
| **Overkill for simple objects** | A two-field object doesn't need a builder |
| **Mutable builder state** | The builder itself is mutable — not safe for concurrent use from multiple goroutines/threads |

## 🎓 When to Use

✅ **Good Candidates:**
- Objects with many fields, especially many optional ones
- Immutability is required on the finished product
- You want meaningful error messages when required fields are missing
- You have multiple "flavours" of the same object (Director templates)
- Constructing the object in one go is impossible (e.g. multi-step initialisation)

❌ **Bad Candidates:**
- Simple objects with one or two fields
- When the product is inherently mutable (use fluent setters instead)
- When all fields are always required (a constructor is cleaner)

## 🔀 Alternatives

| Alternative | When to Use Instead |
|-------------|---------------------|
| **Object initialisers** | Product can be mutable; you don't need enforced construction order |
| **Factory Method (1.2)** | Creating one of several product *types*, not a single complex product |
| **Abstract Factory (1.3)** | Creating *families* of related products, not building one product step-by-step |
| **Prototype (1.5)** | Starting from a known-good instance and cloning, rather than building from scratch |

## 📚 Related Patterns

- **Factory Method (1.2)** — a factory creates entire objects in one call; a builder assembles them step by step
- **Abstract Factory (1.3)** — creates families of objects; builder creates one complex object
- **Prototype (1.5)** — clones an existing object; often used together when the clone needs post-clone customisation
- **Composite (2.3)** — builders are often used to assemble Composite trees (e.g. `HtmlBuilder`)
- **Template Method (3.10)** — Director's template methods parallel Template Method; the steps are delegated to the builder interface

## 🔑 Key Takeaways

1. **`Build()` is the gatekeeping step** — no partial object can be returned before that
2. **The internal constructor is the lock** — only same-assembly code (the builder) can create the product
3. **The Director is optional** — it adds value when you have recurring construction shapes
4. **Reset() unlocks reuse** — one builder instance can produce many independent products
5. **In .NET you already use this daily** — `WebApplicationBuilder`, `DbContextOptionsBuilder`, `SqlConnectionStringBuilder`, and `StringBuilder` all follow this pattern

## 📖 Further Reading

- "Design Patterns: Elements of Reusable Object-Oriented Software" (Gang of Four) — Chapter 3
- "Head First Design Patterns" — Chapter 4

---

← **Previous Pattern:** [1.3 - Abstract Factory](../1.3-AbstractFactory/)
→ **Next Pattern:** [1.5 - Prototype](../1.5-Prototype/)
