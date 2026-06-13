# 3.01 Chain of Responsibility Pattern

## Intent

Pass a request along a chain of handlers. Each handler decides either to process the request or to pass it to the next handler in the chain. The sender does not know which handler will ultimately handle it.

---

## The Problem It Solves

Without this pattern, routing logic is centralised in a single place that must know about every possible handler:

```csharp
// Without Chain of Responsibility — one method knows everything
void RouteTicket(SupportTicket ticket)
{
    if (ticket.Priority == Priority.Low)       { Tier1.Resolve(ticket); return; }
    if (ticket.Priority == Priority.Medium)    { Tier2.Resolve(ticket); return; }
    if (ticket.Priority == Priority.High)      { Tier3.Resolve(ticket); return; }
    if (ticket.Priority == Priority.Critical)  { Oncall.Page(ticket);   return; }
}
// Adding a new level means editing this method — violates Open/Closed
```

With Chain of Responsibility, each handler only knows about itself and its successor. Adding a new tier means adding a new class, not editing existing ones.

---

## Domain: Support Ticket Escalation

| Role | Class | Description |
|------|-------|-------------|
| **Handler interface** | `ITicketHandler` | `Handle(ticket)` and `SetNext(next)` |
| **Abstract base** | `TicketHandlerBase` | Stores `_next`; implements `SetNext`; provides `PassToNext` |
| **Concrete handler** | `Tier1Handler` | Resolves Low priority tickets; escalates anything higher |
| **Concrete handler** | `Tier2Handler` | Resolves Medium priority tickets; escalates anything higher |
| **Concrete handler** | `Tier3Handler` | Resolves High priority tickets; escalates anything higher |
| **Terminal handler** | `OncallHandler` | Handles everything that reaches it — never passes on |

---

## Structure

```
SupportTicket
     │
     ▼
Tier1Handler ──── (Low?)  yes ──▶ resolved
     │ no
     ▼
Tier2Handler ─── (Medium?) yes ──▶ resolved
     │ no
     ▼
Tier3Handler ──── (High?)  yes ──▶ resolved
     │ no
     ▼
OncallHandler ──────────────────▶ paged (always handles)
```

---

## Building the Chain

`SetNext` returns the next handler, enabling fluent assembly:

```csharp
var tier1  = new Tier1Handler();
var tier2  = new Tier2Handler();
var tier3  = new Tier3Handler();
var oncall = new OncallHandler();

tier1.SetNext(tier2).SetNext(tier3).SetNext(oncall);

// Submit to the first link — routing is automatic
tier1.Handle(ticket);
```

The caller only holds a reference to the first handler (`tier1`). The chain is entirely internal.

---

## The Base Class Pattern

All handlers inherit `TicketHandlerBase`, which manages the `_next` reference and the pass-on logic:

```csharp
public abstract class TicketHandlerBase : ITicketHandler
{
    private ITicketHandler? _next;

    // Returns next so callers can chain: a.SetNext(b).SetNext(c)
    public ITicketHandler SetNext(ITicketHandler next)
    {
        _next = next;
        return next;
    }

    public abstract void Handle(SupportTicket ticket);

    protected void PassToNext(SupportTicket ticket)
    {
        if (_next is not null)
            _next.Handle(ticket);
        else
            Console.WriteLine($"[UNRESOLVED] #{ticket.Id} — no handler could process this");
    }
}
```

A concrete handler either resolves the ticket (and returns), or calls `PassToNext`:

```csharp
public sealed class Tier1Handler : TicketHandlerBase
{
    public override void Handle(SupportTicket ticket)
    {
        if (ticket.Priority == Priority.Low)
        {
            Console.WriteLine($"[TIER-1] Resolved #{ticket.Id}");
            return;                 // handled — chain stops here
        }
        PassToNext(ticket);         // not mine — pass it on
    }
}
```

---

## When to Use

- More than one handler could process a request and you don't want to hardcode which one
- The set of handlers or their order needs to change at runtime or by configuration
- You want to decouple the sender from the set of potential receivers

## When NOT to Use

- There is exactly one handler — just call it directly
- Every request must be handled and you need a guarantee — a chain with no terminal handler can silently drop requests
- The selection logic is a simple `switch` that will never change — the pattern adds indirection with no benefit

---

## Benefits

- Adding a new handler doesn't require changing existing handlers (Open/Closed)
- Chain composition is flexible — reorder, add, or remove handlers at runtime
- Each handler has a single responsibility

## Drawbacks

- A request can go unhandled if no handler in the chain matches
- Long chains can be hard to debug — a ticket might pass through many handlers before being resolved
- No guarantee of handling unless you always include a terminal catch-all

---

## Running the Demo

```bash
cd src/3-Behavioral/3.01-ChainOfResponsibility/ChainOfResponsibilityPattern
dotnet run
```

## Running Tests

```bash
cd src/3-Behavioral/3.01-ChainOfResponsibility/ChainOfResponsibilityPattern.Tests
dotnet test
```

---

## Related Patterns

- **Decorator** — also chains objects that implement the same interface, but every link always calls the next; Chain of Responsibility stops when a handler handles the request
- **Command** — encapsulates a request as an object; Chain of Responsibility routes that object through handlers
- **Composite** — a handler could itself be a composite that delegates to child handlers

---

### Chain of Responsibility vs Decorator

The structural similarity is high — both hold a `_next` reference and implement the same interface. The difference is in what happens after each link processes the request:

| | Chain of Responsibility | Decorator |
|---|---|---|
| **After handling** | Chain **stops** — the handler that matches owns the request | Chain **continues** — every decorator runs |
| **Intent** | Route to the right handler | Add behaviour to every call |
| **Typical flow** | One handler handles; rest never see the request | All decorators run in order, always |

```csharp
// Chain of Responsibility — only one handler runs
tier1.Handle(criticalTicket);
// Tier1 passes → Tier2 passes → Tier3 passes → OncallHandler resolves (STOPS)

// Decorator — every link runs
INotifier notifier = new LoggingDecorator(new SmsDecorator(new ConsoleNotifier()));
notifier.Send(...);
// LoggingDecorator runs → SmsDecorator runs → ConsoleNotifier runs (ALL THREE)
```
