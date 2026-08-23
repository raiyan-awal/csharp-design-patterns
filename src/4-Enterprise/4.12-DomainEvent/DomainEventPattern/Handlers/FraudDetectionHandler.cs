using DomainEventPattern.Events;

namespace DomainEventPattern.Handlers;

// Detects suspicious bidding: a bidder raising their own current winning bid
// (shill bidding — artificially inflating price with no intent to compete).
public sealed class FraudDetectionHandler : IDomainEventHandler<BidPlacedEvent>
{
    private readonly Dictionary<int, string?> _currentWinner = new();
    private readonly List<string> _alerts = new();
    public IReadOnlyList<string> Alerts => _alerts;

    public void Handle(BidPlacedEvent e)
    {
        if (_currentWinner.TryGetValue(e.AuctionId, out var previousWinner)
            && previousWinner == e.Bidder)
        {
            var alert = $"[FRAUD] Shill bid detected on auction #{e.AuctionId}: '{e.Bidder}' raised their own winning bid to ${e.Amount:N2} CAD";
            _alerts.Add(alert);
            Console.WriteLine(alert);
        }

        _currentWinner[e.AuctionId] = e.Bidder;
    }
}
