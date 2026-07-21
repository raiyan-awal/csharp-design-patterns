namespace NullObjectPattern.Loggers;

public sealed class ConsoleAuditLogger : IAuditLogger
{
    public void Log(string orderId, string action, string details = "")
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var message   = string.IsNullOrEmpty(details) ? action : $"{action} — {details}";
        Console.WriteLine($"  [AUDIT {timestamp}] Order #{orderId}: {message}");
    }
}
