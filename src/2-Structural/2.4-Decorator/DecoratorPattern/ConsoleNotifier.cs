namespace DecoratorPattern;

// ============================================================
// CONCRETE COMPONENT
// ============================================================
// The base implementation — the real work happens here.
// Simulates sending an email by writing to the console.
// All decorators ultimately delegate here (directly or through
// a chain). This class knows nothing about logging, retries,
// or any other cross-cutting concern.

public sealed class ConsoleNotifier : INotifier
{
    public void Send(string recipient, string subject, string body)
    {
        Console.WriteLine($"[EMAIL] To     : {recipient}");
        Console.WriteLine($"        Subject: {subject}");
        Console.WriteLine($"        Body   : {body}");
    }
}
