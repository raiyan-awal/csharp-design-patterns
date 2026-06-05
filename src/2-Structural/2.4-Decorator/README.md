# 2.4 Decorator Pattern

## Intent

Attach additional responsibilities to an object dynamically. Decorators provide a flexible alternative to subclassing for extending functionality.

---

## The Problem It Solves

You have a notification service that sends emails. Now you need some notifications logged, some retried on failure, some also sent via SMS, and some tagged with an environment prefix — in various combinations.

With inheritance you'd need a class for every combination:

```
EmailNotifier
EmailNotifierWithLogging
EmailNotifierWithSms
EmailNotifierWithLoggingAndSms
EmailNotifierWithLoggingAndRetry
EmailNotifierWithLoggingAndSmsAndRetry
EmailNotifierWithLoggingAndSmsAndRetryAndPrefix
... (M × N explosion)
```

With Decorator you write one class per concern and compose at runtime:

```csharp
INotifier notifier = new SubjectPrefixDecorator(
                         new LoggingDecorator(
                             new SmsDecorator(
                                 new RetryDecorator(
                                     new ConsoleNotifier()))),
                         "[PROD]");

notifier.Send(recipient, subject, body);
// Runs: prefix → log → sms → retry → email
// Each class is ~20 lines and knows nothing about the others
```

---

## Domain: Notification System

| Role | Class | Description |
|------|-------|-------------|
| **Component Interface** | `INotifier` | `Send(recipient, subject, body)` — the contract |
| **Concrete Component** | `ConsoleNotifier` | Base implementation — simulates sending email |
| **Abstract Decorator** | `NotifierDecorator` | Holds `_inner`, delegates by default |
| **Concrete Decorators** | `LoggingDecorator` | Logs before and after the inner call |
| | `RetryDecorator` | Retries up to N times on exception |
| | `SmsDecorator` | Also fires an SMS after the inner channel |
| | `SubjectPrefixDecorator` | Prepends a tag to every subject |

---

## Structure

```
INotifier
    ▲
    ├── ConsoleNotifier          (concrete component)
    └── NotifierDecorator        (abstract base decorator)
            ▲
            ├── LoggingDecorator
            ├── RetryDecorator
            ├── SmsDecorator
            └── SubjectPrefixDecorator
```

Every decorator holds exactly one `INotifier _inner`. Calling `Send()` on the outermost decorator triggers a chain:

```
SubjectPrefixDecorator.Send()
  → modifies subject, calls LoggingDecorator.Send()
      → logs "Sending...", calls SmsDecorator.Send()
          → calls ConsoleNotifier.Send()  (inner channel)
          → fires SMS
      → logs "Delivered"
```

---

## Why an Abstract Base Decorator?

Without it, every concrete decorator would repeat the same constructor and delegation:

```csharp
// Without base class — duplicated in every decorator:
public sealed class LoggingDecorator : INotifier
{
    private readonly INotifier _inner;                            // ← repeated
    public LoggingDecorator(INotifier inner) => _inner = inner;  // ← repeated

    public void Send(...) { /* log */ _inner.Send(...); /* log */ }
}

// With NotifierDecorator base class — only the new behaviour:
public sealed class LoggingDecorator : NotifierDecorator
{
    public LoggingDecorator(INotifier inner) : base(inner) { }

    public override void Send(...) { /* log */ base.Send(...); /* log */ }
}
```

The base class absorbs the boilerplate; concrete decorators only declare what changes.

---

## The Delegation Rule

A decorator must always eventually reach `_inner`. Without this, you're not decorating — you're replacing. `base.Send()` is just shorthand for `_inner.Send()`; `NotifierDecorator.Send()` does nothing but call `_inner.Send()`.

`RetryDecorator` is instructive: it never calls `base.Send()`, but it still delegates — it calls `_inner.Send()` explicitly inside a loop. The rule is not "always call `base.Send()`" but "always eventually reach `_inner`."

**How this differs from Composite:**

| | Decorator | Composite |
|---|---|---|
| Inner reference | exactly **one** `_inner` | **zero or more** children |
| Delegation purpose | enhance one object | aggregate many objects |
| Base case | `ConsoleNotifier` — does the work, no delegation | `File` — does the work, no delegation |
| Composite case | `LoggingDecorator` — delegates to its **one** `_inner` | `Directory` — delegates to **all** children, sums results |

Both patterns have a base case (leaf) that does the real work without delegating. The difference is in the composite node: `Directory` fans out to many children and aggregates; `LoggingDecorator` narrows to exactly one and enhances.

---

## Order of Wrapping Matters

The outermost decorator runs first. Swapping the order changes behaviour:

```csharp
// A) Logging wraps Retry:
new LoggingDecorator(new RetryDecorator(base))
// Log: "Sending..." → (retry silently handles failures) → "Delivered"
// The log sees one call regardless of how many retries happen.

// B) Retry wraps Logging:
new RetryDecorator(new LoggingDecorator(base))
// Each retry attempt triggers its own "Sending..." / "Delivered" log entry.
```

Neither is wrong — choose based on what the log should represent.

---

## When to Use

- You need to add behaviour to individual objects without affecting others of the same class
- You want to combine concerns (logging, retry, caching) freely at runtime
- Subclassing would create an M×N class explosion for M base types and N concerns

## When NOT to Use

- The number of decorators is fixed and always the same — just put the logic in the class
- Decorators accumulate into deeply nested stacks that are hard to trace in a debugger
- You need to inspect or remove a specific decorator from the middle of a chain (requires a different approach)

---

## Benefits

- Open/Closed: add new behaviour without modifying existing classes
- Single Responsibility: each decorator addresses exactly one concern
- Composable: mix and match decorators freely at runtime
- Reversible: not wrapping something is not wrapping it — no state to undo

## Drawbacks

- Deep stacks are harder to debug (stack traces go through every wrapper)
- **Identity:** `notifier is LoggingDecorator` is only `true` if `LoggingDecorator` is the outermost wrapper. If it's buried, the type check misses it:
  ```csharp
  INotifier notifier = new SmsDecorator(new LoggingDecorator(new ConsoleNotifier()));
  notifier is SmsDecorator       // true  — outermost
  notifier is LoggingDecorator   // false — buried inside
  ```
  To find a specific decorator you'd need to walk the chain manually — and `_inner` is `protected`, so you can't even do that from outside. If you ever need to inspect or remove a decorator at runtime, a builder that tracks what it has added is a cleaner fit.
- Order dependency: callers must know which order is correct

---

## Running the Demo

```bash
cd src/2-Structural/2.4-Decorator/DecoratorPattern
dotnet run
```

## Running Tests

```bash
cd src/2-Structural/2.4-Decorator/DecoratorPattern.Tests
dotnet test
```

---

## Related Patterns

- **Strategy** — swaps the algorithm inside an object; Decorator wraps the outside and stacks
- **Composite** — both use recursive composition, but Composite aggregates many children; Decorator enhances exactly one inner object
- **Proxy** — structurally identical (wraps one inner object behind the same interface), but the intent differs: Proxy controls *access*, Decorator adds *behaviour*
- **Chain of Responsibility** — also chains handlers, but each handler decides whether to pass to the next; Decorator always delegates

---

### Decorator vs Strategy

```csharp
// Strategy — swap the algorithm inside the object:
var notifier = new SmartNotifier(strategy: new SmsStrategy());
notifier.Send(...);   // uses SMS strategy
notifier.Strategy = new EmailStrategy();
notifier.Send(...);   // now uses email

// Decorator — wrap from the outside, stack freely:
INotifier notifier = new SmsDecorator(new ConsoleNotifier());
// To add email back: new SmsDecorator(new EmailDecorator(new ConsoleNotifier()))
// Strategies are interchangeable; decorators are additive
```

---

### Decorator vs Proxy

Structurally identical — one object wrapping another behind the same interface. The difference is intent:

```csharp
// Proxy — controls ACCESS to the inner object:
public sealed class AuthNotifierProxy : INotifier
{
    private readonly INotifier _inner;
    private readonly IAuthService _auth;

    public void Send(string recipient, string subject, string body)
    {
        if (!_auth.IsAuthorised(recipient))
            throw new UnauthorisedAccessException();
        _inner.Send(recipient, subject, body);   // may or may not reach inner
    }
}

// Decorator — adds BEHAVIOUR, always reaches inner:
public sealed class LoggingDecorator : NotifierDecorator
{
    public override void Send(string recipient, string subject, string body)
    {
        _log("Sending...");
        base.Send(recipient, subject, body);   // always delegates
        _log("Delivered");
    }
}
```

Proxy may block the call entirely. Decorator always passes through — it just adds something on the way.

**Without access control, is there a difference?**

Very little at the code level — the GoF acknowledge this themselves. The remaining differences are intent and lifecycle.

A Proxy is often created to *represent* something: an object that doesn't exist yet, is expensive to initialize, or lives on a remote server. The proxy controls **when and whether** the real object is touched.

```csharp
// Lazy proxy — controls lifecycle; the real object doesn't exist until first use:
public sealed class LazyNotifierProxy : INotifier
{
    private ConsoleNotifier? _real;

    public void Send(string r, string s, string b)
    {
        _real ??= new ConsoleNotifier();   // created on demand
        _real.Send(r, s, b);
    }
}

// Decorator — the inner object is alive and passed in by the caller:
public sealed class LoggingDecorator : NotifierDecorator
{
    public LoggingDecorator(INotifier inner) : base(inner) { }  // inner already exists
}
```

A Decorator is created to *enhance* something that already exists and is handed to it. It doesn't control the lifecycle of `_inner`.

**Summary:** if the class controls whether or how its inner object is created or reached — it's a Proxy. If it just adds behaviour around something it was given — it's a Decorator. With no access control and no lifecycle concern, the two are structurally indistinguishable.
