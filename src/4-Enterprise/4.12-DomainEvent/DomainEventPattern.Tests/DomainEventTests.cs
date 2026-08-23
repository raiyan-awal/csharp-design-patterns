using DomainEventPattern.Domain;
using DomainEventPattern.Events;
using DomainEventPattern.Handlers;
using DomainEventPattern.Infrastructure;

namespace DomainEventPattern.Tests;

public sealed class AggregateRootTests
{
    [Fact]
    public void Auction_RaisesAuctionOpenedEvent_OnConstruction()
    {
        var auction = new Auction(1, "Lawren Harris Landscape", 500_000m);

        Assert.Single(auction.DomainEvents);
        Assert.IsType<AuctionOpenedEvent>(auction.DomainEvents[0]);
    }

    [Fact]
    public void Auction_AuctionOpenedEvent_HasCorrectPayload()
    {
        var auction = new Auction(7, "Inuit Sculpture", 12_000m);

        var e = Assert.IsType<AuctionOpenedEvent>(auction.DomainEvents[0]);
        Assert.Equal(7, e.AuctionId);
        Assert.Equal("Inuit Sculpture", e.Title);
        Assert.Equal(12_000m, e.ReservePrice);
    }

    [Fact]
    public void PlaceBid_RaisesBidPlacedEvent()
    {
        var auction = new Auction(1, "Painting", 1_000m);
        auction.ClearEvents();

        auction.PlaceBid("Alice", 1_500m);

        Assert.Single(auction.DomainEvents);
        Assert.IsType<BidPlacedEvent>(auction.DomainEvents[0]);
    }

    [Fact]
    public void PlaceBid_BidPlacedEvent_HasCorrectPayload()
    {
        var auction = new Auction(3, "Sculpture", 5_000m);
        auction.ClearEvents();

        auction.PlaceBid("Bob Tremblay", 6_000m);

        var e = Assert.IsType<BidPlacedEvent>(auction.DomainEvents[0]);
        Assert.Equal(3, e.AuctionId);
        Assert.Equal("Bob Tremblay", e.Bidder);
        Assert.Equal(6_000m, e.Amount);
        Assert.Equal(1, e.BidNumber);
    }

    [Fact]
    public void PlaceBid_MultipleBids_BidNumberIncrementsCorrectly()
    {
        var auction = new Auction(1, "Painting", 1_000m);
        auction.ClearEvents();

        auction.PlaceBid("Alice", 2_000m);
        auction.PlaceBid("Bob",   3_000m);
        auction.PlaceBid("Alice", 4_000m);

        var events = auction.DomainEvents.OfType<BidPlacedEvent>().ToList();
        Assert.Equal(1, events[0].BidNumber);
        Assert.Equal(2, events[1].BidNumber);
        Assert.Equal(3, events[2].BidNumber);
    }

    [Fact]
    public void PlaceBid_TooLow_Throws_NoEventRaised()
    {
        var auction = new Auction(1, "Painting", 1_000m);
        auction.PlaceBid("Alice", 2_000m);
        int eventsBefore = auction.DomainEvents.Count;

        Assert.Throws<InvalidOperationException>(() => auction.PlaceBid("Bob", 1_500m));
        Assert.Equal(eventsBefore, auction.DomainEvents.Count);
    }

    [Fact]
    public void PlaceBid_OnClosedAuction_Throws()
    {
        var auction = new Auction(1, "Painting", 1_000m);
        auction.PlaceBid("Alice", 2_000m);
        auction.Close();

        Assert.Throws<InvalidOperationException>(() => auction.PlaceBid("Bob", 3_000m));
    }

    [Fact]
    public void Close_RaisesAuctionClosedEvent()
    {
        var auction = new Auction(1, "Painting", 1_000m);
        auction.PlaceBid("Alice", 2_000m);
        auction.ClearEvents();

        auction.Close();

        Assert.Single(auction.DomainEvents);
        Assert.IsType<AuctionClosedEvent>(auction.DomainEvents[0]);
    }

    [Fact]
    public void Close_WhenReserveMet_ReserveMetIsTrue()
    {
        var auction = new Auction(1, "Painting", 1_000m);
        auction.PlaceBid("Alice", 1_500m);
        auction.Close();

        var e = auction.DomainEvents.OfType<AuctionClosedEvent>().Single();
        Assert.True(e.ReserveMet);
        Assert.Equal("Alice", e.Winner);
        Assert.Equal(1_500m, e.WinningBid);
    }

    [Fact]
    public void Close_WhenReserveNotMet_ReserveMetIsFalse()
    {
        var auction = new Auction(1, "Painting", 50_000m);
        auction.PlaceBid("Bob", 30_000m);
        auction.Close();

        var e = auction.DomainEvents.OfType<AuctionClosedEvent>().Single();
        Assert.False(e.ReserveMet);
    }

    [Fact]
    public void Close_NoBids_WinnerIsNull_ReserveNotMet()
    {
        var auction = new Auction(1, "Painting", 10_000m);
        auction.Close();

        var e = auction.DomainEvents.OfType<AuctionClosedEvent>().Single();
        Assert.Null(e.Winner);
        Assert.False(e.ReserveMet);
    }

    [Fact]
    public void ClearEvents_EmptiesDomainEventsList()
    {
        var auction = new Auction(1, "Painting", 1_000m);
        auction.PlaceBid("Alice", 2_000m);

        auction.ClearEvents();

        Assert.Empty(auction.DomainEvents);
    }
}

public sealed class DispatcherTests
{
    private static Auction MakeAuction() => new(1, "Lawren Harris Landscape", 500_000m);

    [Fact]
    public void Dispatcher_RoutesEvent_ToCorrectHandler()
    {
        var dispatcher = new DomainEventDispatcher();
        var audit      = new AuditLogHandler();
        dispatcher.Register<AuctionOpenedEvent>(audit);

        var auction = MakeAuction();
        dispatcher.Dispatch(auction.DomainEvents);

        Assert.Single(audit.Log);
    }

    [Fact]
    public void Dispatcher_MultipleHandlers_ForSameEvent_AllFired()
    {
        var dispatcher = new DomainEventDispatcher();
        var audit1     = new AuditLogHandler();
        var audit2     = new AuditLogHandler();
        dispatcher.Register<AuctionOpenedEvent>(audit1);
        dispatcher.Register<AuctionOpenedEvent>(audit2);

        var auction = MakeAuction();
        dispatcher.Dispatch(auction.DomainEvents);

        Assert.Single(audit1.Log);
        Assert.Single(audit2.Log);
    }

    [Fact]
    public void Dispatcher_NoHandlerRegistered_DoesNotThrow()
    {
        var dispatcher = new DomainEventDispatcher();
        var auction    = MakeAuction();

        var ex = Record.Exception(() => dispatcher.Dispatch(auction.DomainEvents));
        Assert.Null(ex);
    }

    [Fact]
    public void Dispatcher_DispatchAndClear_ClearsEventsAfterDispatch()
    {
        var dispatcher = new DomainEventDispatcher();
        var audit      = new AuditLogHandler();
        dispatcher.Register<AuctionOpenedEvent>(audit);

        var auction = MakeAuction();
        dispatcher.DispatchAndClear(auction);

        Assert.Empty(auction.DomainEvents);
        Assert.Single(audit.Log);
    }
}

public sealed class HandlerTests
{
    [Fact]
    public void AuditLogHandler_LogsAllThreeEventTypes()
    {
        var dispatcher = new DomainEventDispatcher();
        var audit      = new AuditLogHandler();
        dispatcher.Register<AuctionOpenedEvent>(audit);
        dispatcher.Register<BidPlacedEvent>(audit);
        dispatcher.Register<AuctionClosedEvent>(audit);

        var auction = new Auction(1, "Painting", 1_000m);
        auction.PlaceBid("Alice", 2_000m);
        auction.Close();
        dispatcher.Dispatch(auction.DomainEvents);

        Assert.Equal(3, audit.Log.Count);
        Assert.Contains(audit.Log, l => l.Contains("opened"));
        Assert.Contains(audit.Log, l => l.Contains("Bid #1"));
        Assert.Contains(audit.Log, l => l.Contains("closed"));
    }

    [Fact]
    public void EmailHandler_SendsEmail_OnBidPlaced()
    {
        var dispatcher = new DomainEventDispatcher();
        var email      = new EmailNotificationHandler();
        dispatcher.Register<BidPlacedEvent>(email);

        var auction = new Auction(1, "Sculpture", 5_000m);
        auction.PlaceBid("Priya Nair", 6_500m);
        dispatcher.Dispatch(auction.DomainEvents);

        Assert.Single(email.SentEmails);
        Assert.Contains("Priya Nair", email.SentEmails[0]);
    }

    [Fact]
    public void FraudHandler_RaisesAlert_WhenWinnerRaisesOwnBid()
    {
        var dispatcher = new DomainEventDispatcher();
        var fraud      = new FraudDetectionHandler();
        dispatcher.Register<BidPlacedEvent>(fraud);

        var auction = new Auction(1, "Painting", 1_000m);
        auction.PlaceBid("Sandra Chu", 2_000m);
        auction.PlaceBid("Marcus",     3_000m);
        auction.PlaceBid("Sandra Chu", 4_000m);  // outbid, then bids again — not shill
        auction.PlaceBid("Sandra Chu", 5_000m);  // now raising own winning bid — shill

        dispatcher.Dispatch(auction.DomainEvents);

        Assert.Single(fraud.Alerts);
        Assert.Contains("Sandra Chu", fraud.Alerts[0]);
    }

    [Fact]
    public void FraudHandler_NoAlert_WhenDifferentBidderOutbids()
    {
        var dispatcher = new DomainEventDispatcher();
        var fraud      = new FraudDetectionHandler();
        dispatcher.Register<BidPlacedEvent>(fraud);

        var auction = new Auction(1, "Painting", 1_000m);
        auction.PlaceBid("Alice", 2_000m);
        auction.PlaceBid("Bob",   3_000m);
        auction.PlaceBid("Alice", 4_000m);

        dispatcher.Dispatch(auction.DomainEvents);

        Assert.Empty(fraud.Alerts);
    }
}
