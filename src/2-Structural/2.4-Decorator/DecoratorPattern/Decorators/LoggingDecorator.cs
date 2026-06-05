namespace DecoratorPattern;

// Logs every notification attempt — before and after the inner call.
// The log action is injectable so callers and tests can redirect output
// without touching this class.
public sealed class LoggingDecorator : NotifierDecorator
{
    private readonly Action<string> _log;

    public LoggingDecorator(INotifier inner, Action<string>? log = null) : base(inner)
        => _log = log ?? Console.WriteLine;

    public override void Send(string recipient, string subject, string body)
    {
        _log($"[LOG] Sending to {recipient}: \"{subject}\"");
        base.Send(recipient, subject, body);
        _log($"[LOG] Delivered to {recipient}");
    }
}
