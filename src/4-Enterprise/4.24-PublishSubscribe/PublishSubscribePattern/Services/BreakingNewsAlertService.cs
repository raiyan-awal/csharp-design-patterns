using PublishSubscribePattern.Events;

namespace PublishSubscribePattern.Services;

public sealed class BreakingNewsAlertService
{
    private readonly List<string> _alertsSent = [];

    public IReadOnlyList<string> AlertsSent => _alertsSent;

    public void OnBreakingNewsAlert(BreakingNewsAlertEvent @event)
    {
        _alertsSent.Add(@event.AlertHeadline);
        Console.WriteLine($"[Breaking Alert] PUSH: {@event.AlertHeadline}");
    }
}
