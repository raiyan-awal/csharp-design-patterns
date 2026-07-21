namespace NullObjectPattern.Loggers;

// Null Object — implements IAuditLogger with a safe no-op.
// Inject this when audit logging should be suppressed (automated pipelines,
// unit tests, or environments where no audit sink is configured).
// Singleton: stateless, one instance is sufficient for the entire application.
public sealed class NullAuditLogger : IAuditLogger
{
    public static readonly NullAuditLogger Instance = new();
    private NullAuditLogger() { }

    public void Log(string orderId, string action, string details = "") { }
}
