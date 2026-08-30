using OutboxPattern.Core;

namespace OutboxPattern.Services;

public sealed class SimulatedEmailHandler
{
    public List<string> ReceivedEvents { get; } = [];

    public void Handle(OutboxMessage message)
        => ReceivedEvents.Add($"[Email] {message.EventType} — order {ExtractOrderId(message.Payload)}");

    private static string ExtractOrderId(string payload)
    {
        const string marker = "\"orderId\":\"";
        var start = payload.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) return "?";
        start += marker.Length;
        var end = payload.IndexOf('"', start);
        return end > start ? payload[start..end] : "?";
    }
}

public sealed class SimulatedInventoryHandler
{
    private bool _failNext;

    public List<string> ReceivedEvents { get; } = [];

    public void FailOnNextCall() => _failNext = true;

    public void Handle(OutboxMessage message)
    {
        if (_failNext)
        {
            _failNext = false;
            throw new InvalidOperationException("Inventory service temporarily unavailable.");
        }
        ReceivedEvents.Add($"[Inventory] Reserved stock for {message.EventType}");
    }
}
