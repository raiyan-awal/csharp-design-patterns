namespace MediatorPattern;

public interface ISmartHomeHub
{
    void Register(ISmartDevice device);
    void Send(string eventType, string source, string? payload = null);
}
