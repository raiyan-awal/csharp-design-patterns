using DomainEventPattern.Events;

namespace DomainEventPattern.Domain;

public enum AuctionStatus { Open, Closed }

public sealed class Auction : AggregateRoot
{
    private readonly List<(string Bidder, decimal Amount)> _bids = new();

    public int           Id            { get; }
    public string        Title         { get; }
    public decimal       ReservePrice  { get; }
    public decimal       CurrentBid    { get; private set; }
    public string?       CurrentWinner { get; private set; }
    public AuctionStatus Status        { get; private set; }
    public int           BidCount      => _bids.Count;

    public Auction(int id, string title, decimal reservePrice)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (reservePrice <= 0)
            throw new ArgumentException("Reserve price must be positive.", nameof(reservePrice));

        Id           = id;
        Title        = title;
        ReservePrice = reservePrice;
        Status       = AuctionStatus.Open;

        Raise(new AuctionOpenedEvent(id, title, reservePrice, DateTime.UtcNow));
    }

    public void PlaceBid(string bidder, decimal amount)
    {
        if (Status != AuctionStatus.Open)
            throw new InvalidOperationException("Bids cannot be placed on a closed auction.");
        if (string.IsNullOrWhiteSpace(bidder))
            throw new ArgumentException("Bidder name is required.", nameof(bidder));
        if (amount <= CurrentBid)
            throw new InvalidOperationException(
                $"Bid of ${amount:N2} must exceed the current bid of ${CurrentBid:N2} CAD.");

        _bids.Add((bidder, amount));
        CurrentBid    = amount;
        CurrentWinner = bidder;

        Raise(new BidPlacedEvent(Id, bidder, amount, _bids.Count, DateTime.UtcNow));
    }

    public void Close()
    {
        if (Status != AuctionStatus.Open)
            throw new InvalidOperationException("Auction is already closed.");

        Status = AuctionStatus.Closed;
        bool reserveMet = CurrentWinner is not null && CurrentBid >= ReservePrice;

        Raise(new AuctionClosedEvent(Id, Title, CurrentWinner, CurrentBid, reserveMet, DateTime.UtcNow));
    }
}
