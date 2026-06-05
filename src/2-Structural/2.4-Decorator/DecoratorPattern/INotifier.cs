namespace DecoratorPattern;

// ============================================================
// COMPONENT INTERFACE
// ============================================================
// Both the concrete component (ConsoleNotifier) and every
// decorator implement this interface. The client always holds
// an INotifier — it never knows whether it's talking to the
// base implementation or a decorated chain.

public interface INotifier
{
    void Send(string recipient, string subject, string body);
}
