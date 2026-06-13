using ChainOfResponsibilityPattern;

// ============================================================
// CHAIN OF RESPONSIBILITY PATTERN — DEMO
// ============================================================
// Support tickets are routed through a chain of handlers.
// Each handler resolves the ticket if it can; otherwise it
// passes it to the next handler in the chain.
//
// Chain: Tier-1 → Tier-2 → Tier-3 → On-Call
// ============================================================

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine("\n");
}

static ITicketHandler BuildChain()
{
    var tier1  = new Tier1Handler();
    var tier2  = new Tier2Handler();
    var tier3  = new Tier3Handler();
    var oncall = new OncallHandler();

    // SetNext returns the next handler, enabling fluent chaining
    tier1.SetNext(tier2).SetNext(tier3).SetNext(oncall);
    return tier1;
}

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║  CHAIN OF RESPONSIBILITY — Support Ticket Escalation ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("Chain: Tier-1 (Low) → Tier-2 (Medium) → Tier-3 (High) → On-Call (Critical)");
Console.WriteLine();
Console.WriteLine("Each handler resolves the ticket if it matches its level;");
Console.WriteLine("otherwise it passes the ticket to the next handler.");

Pause();

// ── DEMO 1: Low priority — resolved at Tier-1 ────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 1 — Low priority ticket: resolved immediately at Tier-1");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var chain = BuildChain();
var ticket1 = new SupportTicket("T-001", "Cannot log in to account", Priority.Low, Category.Account);

Console.WriteLine($"Submitting: #{ticket1.Id} [{ticket1.Priority}] {ticket1.Subject}");
Console.WriteLine();
chain.Handle(ticket1);

Pause();

// ── DEMO 2: Medium priority — passes Tier-1, resolved at Tier-2
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 2 — Medium priority: passes Tier-1, resolved at Tier-2");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var ticket2 = new SupportTicket("T-002", "Billing charge appears twice", Priority.Medium, Category.Billing);

Console.WriteLine($"Submitting: #{ticket2.Id} [{ticket2.Priority}] {ticket2.Subject}");
Console.WriteLine();
chain.Handle(ticket2);

Pause();

// ── DEMO 3: High priority — passes Tier-1 and Tier-2, resolved at Tier-3
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 3 — High priority: passes Tier-1 and Tier-2, resolved at Tier-3");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var ticket3 = new SupportTicket("T-003", "API returning 500 on all requests", Priority.High, Category.Technical);

Console.WriteLine($"Submitting: #{ticket3.Id} [{ticket3.Priority}] {ticket3.Subject}");
Console.WriteLine();
chain.Handle(ticket3);

Pause();

// ── DEMO 4: Critical — passes all tiers, On-Call is paged ────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 4 — Critical outage: passes all tiers, On-Call is paged");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

var ticket4 = new SupportTicket("T-004", "Production database unreachable", Priority.Critical, Category.Outage);

Console.WriteLine($"Submitting: #{ticket4.Id} [{ticket4.Priority}] {ticket4.Subject}");
Console.WriteLine();
chain.Handle(ticket4);

Pause();

// ── DEMO 5: No handler — chain has no On-Call at the end ─────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 5 — Unhandled ticket: chain ends with no matching handler");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

// A short chain that only handles Low and Medium — Critical falls through
var shortChain = new Tier1Handler();
shortChain.SetNext(new Tier2Handler());

var ticket5 = new SupportTicket("T-005", "All services down", Priority.Critical, Category.Outage);

Console.WriteLine($"Submitting: #{ticket5.Id} [{ticket5.Priority}] {ticket5.Subject}");
Console.WriteLine("(chain only has Tier-1 → Tier-2, no On-Call)");
Console.WriteLine();
shortChain.Handle(ticket5);

Pause();

// ── DEMO 6: Multiple tickets processed in order ───────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 6 — Batch of tickets routed through the same chain");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

SupportTicket[] tickets =
[
    new("T-101", "Password reset not working",     Priority.Low,      Category.Account),
    new("T-102", "Invoice missing from dashboard", Priority.Medium,   Category.Billing),
    new("T-103", "Authentication service slow",    Priority.High,     Category.Technical),
    new("T-104", "Complete payment outage",        Priority.Critical, Category.Outage),
    new("T-105", "Profile picture not uploading",  Priority.Low,      Category.Account),
];

foreach (var t in tickets)
{
    Console.WriteLine($"Submitting: #{t.Id} [{t.Priority,-8}] {t.Subject}");
    chain.Handle(t);
    Console.WriteLine();
}

Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("Summary:");
Console.WriteLine("  • Each handler checks if it can process the ticket");
Console.WriteLine("  • If yes  → handle and stop; ticket does not continue");
Console.WriteLine("  • If no   → call PassToNext(); next handler tries");
Console.WriteLine("  • If none → PassToNext() logs 'UNRESOLVED'");
Console.WriteLine("  • SetNext returns the next handler for fluent chaining");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
