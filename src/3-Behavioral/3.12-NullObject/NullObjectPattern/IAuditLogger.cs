namespace NullObjectPattern;

// Abstraction for recording order lifecycle events to an audit trail.
// The empty-string default on `details` lets callers omit it for simple actions.
public interface IAuditLogger
{
    void Log(string orderId, string action, string details = "");
}
