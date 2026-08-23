using DomainEventPattern.Events;

namespace DomainEventPattern.Handlers;

// Handles all three event types — builds an immutable audit trail.
public sealed class AuditLogHandler :
    IDomainEventHandler<AuctionOpenedEvent>,
    IDomainEventHandler<BidPlacedEvent>,
    IDomainEventHandler<AuctionClosedEvent>
{
    private readonly List<string> _log = new();
    public IReadOnlyList<string> Log => _log;

    public void Handle(AuctionOpenedEvent e)
    {
        var entry = $"[AUDIT {e.OccurredAt:HH:mm:ss}] Auction #{e.AuctionId} '{e.Title}' opened (reserve ${e.ReservePrice:N2} CAD)";
        _log.Add(entry);
        Console.WriteLine(entry);
    }

    public void Handle(BidPlacedEvent e)
    {
        var entry = $"[AUDIT {e.OccurredAt:HH:mm:ss}] Bid #{e.BidNumber} on auction #{e.AuctionId}: {e.Bidder} — ${e.Amount:N2} CAD";
        _log.Add(entry);
        Console.WriteLine(entry);
    }

    public void Handle(AuctionClosedEvent e)
    {
        var result = e.ReserveMet ? $"sold to {e.Winner} at ${e.WinningBid:N2} CAD" : "reserve not met — no sale";
        var entry  = $"[AUDIT {e.OccurredAt:HH:mm:ss}] Auction #{e.AuctionId} '{e.Title}' closed — {result}";
        _log.Add(entry);
        Console.WriteLine(entry);
    }
}
