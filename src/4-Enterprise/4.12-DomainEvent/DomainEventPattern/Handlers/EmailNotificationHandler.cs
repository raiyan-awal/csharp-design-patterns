using DomainEventPattern.Events;

namespace DomainEventPattern.Handlers;

// Handles BidPlaced and AuctionClosed — simulates outbound email notifications.
public sealed class EmailNotificationHandler :
    IDomainEventHandler<BidPlacedEvent>,
    IDomainEventHandler<AuctionClosedEvent>
{
    private readonly List<string> _sent = new();
    public IReadOnlyList<string> SentEmails => _sent;

    public void Handle(BidPlacedEvent e)
    {
        var msg = $"[EMAIL] New bid on auction {e.AuctionId}: {e.Bidder} bid ${e.Amount:N2} CAD (bid #{e.BidNumber})";
        _sent.Add(msg);
        Console.WriteLine(msg);
    }

    public void Handle(AuctionClosedEvent e)
    {
        var msg = e.ReserveMet
            ? $"[EMAIL] Auction '{e.Title}' closed — winner: {e.Winner} at ${e.WinningBid:N2} CAD"
            : $"[EMAIL] Auction '{e.Title}' closed — reserve not met (highest: ${e.WinningBid:N2} CAD)";
        _sent.Add(msg);
        Console.WriteLine(msg);
    }
}
