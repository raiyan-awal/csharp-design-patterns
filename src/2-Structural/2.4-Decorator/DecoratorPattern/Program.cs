using DecoratorPattern;

// ============================================================
// DECORATOR PATTERN — DEMO
// ============================================================
// Shows how INotifier implementations are wrapped in decorators
// to add cross-cutting concerns (logging, retry, SMS, tagging)
// without modifying ConsoleNotifier or any other class.
// ============================================================

static void Pause()
{
    Console.WriteLine();
    Console.Write("Press any key to continue...");
    Console.ReadKey(intercept: true);
    Console.WriteLine("\n");
}

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║        DECORATOR PATTERN — Notification Chain        ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("Attaches responsibilities to an INotifier dynamically.");
Console.WriteLine("Each decorator adds exactly one concern and delegates");
Console.WriteLine("the rest to whatever is wrapped inside it.");

Pause();

// ── DEMO 1: Base notifier, no decorators ─────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 1 — Concrete component only (no decorators)");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();

INotifier notifier = new ConsoleNotifier();
notifier.Send("alice@example.com", "Order Shipped", "Your order #1042 is on its way.");

Pause();

// ── DEMO 2: Add logging ───────────────────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 2 — Wrap with LoggingDecorator");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("ConsoleNotifier is unchanged — logging is added around it.");
Console.WriteLine();

notifier = new LoggingDecorator(new ConsoleNotifier());
notifier.Send("alice@example.com", "Order Shipped", "Your order #1042 is on its way.");

Pause();

// ── DEMO 3: Add second channel (SMS) ─────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 3 — Wrap with SmsDecorator (two channels)");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("One Send() call now reaches email AND SMS.");
Console.WriteLine();

notifier = new SmsDecorator(new ConsoleNotifier());
notifier.Send("alice@example.com", "Order Shipped", "Your order #1042 is on its way.");

Pause();

// ── DEMO 4: Stack multiple decorators ────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 4 — Stack: Prefix → Logging → SMS → Email");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("Decorators compose. Each adds one concern; together");
Console.WriteLine("they form a chain that runs outermost-first.");
Console.WriteLine();

notifier = new SubjectPrefixDecorator(
               new LoggingDecorator(
                   new SmsDecorator(
                       new ConsoleNotifier())),
               "[PROD]");

notifier.Send("bob@example.com", "Password Reset", "Click the link to reset your password.");

Pause();

// ── DEMO 5: RetryDecorator with simulated flaky inner ────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 5 — RetryDecorator handles transient failures");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("Inner notifier fails twice, succeeds on the third attempt.");
Console.WriteLine();

int callCount = 0;
var flakyInner = new DelegateNotifier((r, s, b) =>
{
    callCount++;
    if (callCount < 3)
        throw new Exception("Network timeout");
    new ConsoleNotifier().Send(r, s, b);
});

notifier = new RetryDecorator(flakyInner, maxAttempts: 3);
notifier.Send("carol@example.com", "Invoice Ready", "Your monthly invoice is ready.");

Pause();

// ── DEMO 6: Order of wrapping matters ────────────────────────
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("DEMO 6 — Order of wrapping changes behaviour");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine();
Console.WriteLine("  A) new LoggingDecorator(new RetryDecorator(base))");
Console.WriteLine("     Logging sees one call. Retry attempts are internal.");
Console.WriteLine("     → Log shows: \"Sending\" ... 3 retries ... \"Delivered\"");
Console.WriteLine();
Console.WriteLine("  B) new RetryDecorator(new LoggingDecorator(base))");
Console.WriteLine("     Every retry attempt triggers a new log entry.");
Console.WriteLine("     → Log shows: \"Sending\" / \"Delivered\" repeated per attempt");
Console.WriteLine();
Console.WriteLine("Neither is wrong — choose based on what you want recorded.");

Console.WriteLine();
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("Summary:");
Console.WriteLine("  • ConsoleNotifier  — concrete component, the real work");
Console.WriteLine("  • NotifierDecorator — base class: holds _inner, delegates");
Console.WriteLine("  • Each decorator adds exactly one cross-cutting concern");
Console.WriteLine("  • Stacking composes behaviour; order controls the sequence");
Console.WriteLine("  • The client always holds INotifier — no type-checking needed");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

// ─────────────────────────────────────────────────────────────
// Local helper — wraps a delegate as an INotifier for demo use
// ─────────────────────────────────────────────────────────────
sealed class DelegateNotifier(Action<string, string, string> action) : INotifier
{
    public void Send(string recipient, string subject, string body)
        => action(recipient, subject, body);
}
