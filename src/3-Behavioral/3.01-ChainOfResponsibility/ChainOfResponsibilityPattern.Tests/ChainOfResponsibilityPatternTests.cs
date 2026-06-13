using ChainOfResponsibilityPattern;

namespace ChainOfResponsibilityPattern.Tests;

// ── Test doubles ──────────────────────────────────────────────

// Records the last ticket it received; does not pass to next
file sealed class RecordingHandler : TicketHandlerBase
{
    public SupportTicket? LastHandled { get; private set; }
    public int HandleCallCount        { get; private set; }

    public override void Handle(SupportTicket ticket)
    {
        LastHandled = ticket;
        HandleCallCount++;
    }
}

// Always passes to next — never handles
file sealed class PassThroughHandler : TicketHandlerBase
{
    public int HandleCallCount { get; private set; }

    public override void Handle(SupportTicket ticket)
    {
        HandleCallCount++;
        PassToNext(ticket);
    }
}

// ── TicketHandlerBase ─────────────────────────────────────────

public class TicketHandlerBaseTests
{
    private static SupportTicket Ticket(Priority p = Priority.Low)
        => new("T-1", "Test ticket", p, Category.Account);

    [Fact]
    public void SetNext_ReturnsSameNextHandler_EnablingFluentChaining()
    {
        var first  = new PassThroughHandler();
        var second = new RecordingHandler();

        var returned = first.SetNext(second);

        Assert.Same(second, returned);
    }

    [Fact]
    public void PassToNext_WhenNextExists_CallsNextHandler()
    {
        var first  = new PassThroughHandler();
        var second = new RecordingHandler();
        first.SetNext(second);

        first.Handle(Ticket());

        Assert.Equal(1, second.HandleCallCount);
    }

    [Fact]
    public void PassToNext_WhenNoNext_DoesNotThrow()
    {
        var handler = new PassThroughHandler();
        var ex = Record.Exception(() => handler.Handle(Ticket()));
        Assert.Null(ex);
    }

    [Fact]
    public void FluentChain_RoutesCorrectly()
    {
        var a = new PassThroughHandler();
        var b = new PassThroughHandler();
        var c = new RecordingHandler();

        a.SetNext(b).SetNext(c);

        var ticket = Ticket();
        a.Handle(ticket);

        Assert.Same(ticket, c.LastHandled);
    }
}

// ── Tier1Handler ──────────────────────────────────────────────

public class Tier1HandlerTests
{
    private static SupportTicket Ticket(Priority p, Category c = Category.Account)
        => new("T-1", "Subject", p, c);

    [Fact]
    public void Handle_LowPriority_ResolvesWithoutPassingOn()
    {
        var next    = new RecordingHandler();
        var handler = new Tier1Handler();
        handler.SetNext(next);

        handler.Handle(Ticket(Priority.Low));

        Assert.Equal(0, next.HandleCallCount);
    }

    [Theory]
    [InlineData(Priority.Medium)]
    [InlineData(Priority.High)]
    [InlineData(Priority.Critical)]
    public void Handle_AboveLow_PassesToNext(Priority priority)
    {
        var next    = new RecordingHandler();
        var handler = new Tier1Handler();
        handler.SetNext(next);

        handler.Handle(Ticket(priority));

        Assert.Equal(1, next.HandleCallCount);
    }

    [Fact]
    public void Handle_PassesOriginalTicket_Unmodified()
    {
        var next    = new RecordingHandler();
        var handler = new Tier1Handler();
        handler.SetNext(next);

        var ticket = Ticket(Priority.Medium);
        handler.Handle(ticket);

        Assert.Same(ticket, next.LastHandled);
    }
}

// ── Tier2Handler ──────────────────────────────────────────────

public class Tier2HandlerTests
{
    private static SupportTicket Ticket(Priority p)
        => new("T-2", "Subject", p, Category.Billing);

    [Fact]
    public void Handle_MediumPriority_ResolvesWithoutPassingOn()
    {
        var next    = new RecordingHandler();
        var handler = new Tier2Handler();
        handler.SetNext(next);

        handler.Handle(Ticket(Priority.Medium));

        Assert.Equal(0, next.HandleCallCount);
    }

    [Theory]
    [InlineData(Priority.Low)]
    [InlineData(Priority.High)]
    [InlineData(Priority.Critical)]
    public void Handle_NotMedium_PassesToNext(Priority priority)
    {
        var next    = new RecordingHandler();
        var handler = new Tier2Handler();
        handler.SetNext(next);

        handler.Handle(Ticket(priority));

        Assert.Equal(1, next.HandleCallCount);
    }
}

// ── Tier3Handler ──────────────────────────────────────────────

public class Tier3HandlerTests
{
    private static SupportTicket Ticket(Priority p)
        => new("T-3", "Subject", p, Category.Technical);

    [Fact]
    public void Handle_HighPriority_ResolvesWithoutPassingOn()
    {
        var next    = new RecordingHandler();
        var handler = new Tier3Handler();
        handler.SetNext(next);

        handler.Handle(Ticket(Priority.High));

        Assert.Equal(0, next.HandleCallCount);
    }

    [Theory]
    [InlineData(Priority.Low)]
    [InlineData(Priority.Medium)]
    [InlineData(Priority.Critical)]
    public void Handle_NotHigh_PassesToNext(Priority priority)
    {
        var next    = new RecordingHandler();
        var handler = new Tier3Handler();
        handler.SetNext(next);

        handler.Handle(Ticket(priority));

        Assert.Equal(1, next.HandleCallCount);
    }
}

// ── OncallHandler ─────────────────────────────────────────────

public class OncallHandlerTests
{
    [Theory]
    [InlineData(Priority.Low)]
    [InlineData(Priority.Medium)]
    [InlineData(Priority.High)]
    [InlineData(Priority.Critical)]
    public void Handle_AnyPriority_ResolvesWithoutPassingOn(Priority priority)
    {
        var next    = new RecordingHandler();
        var handler = new OncallHandler();
        handler.SetNext(next);

        handler.Handle(new SupportTicket("T-4", "Subject", priority, Category.Outage));

        // On-call is the terminal handler — never passes on
        Assert.Equal(0, next.HandleCallCount);
    }
}

// ── Full chain integration ─────────────────────────────────────

public class ChainIntegrationTests
{
    [Fact]
    public void LowPriority_ReachesOnlyTier1()
    {
        var t1 = new RecordingHandler();
        var t2 = new RecordingHandler();
        var t3 = new RecordingHandler();
        var oc = new RecordingHandler();
        t1.SetNext(t2).SetNext(t3).SetNext(oc);

        t1.Handle(new SupportTicket("T-1", "s", Priority.Low, Category.Account));

        Assert.Equal(1, t1.HandleCallCount);
        Assert.Equal(0, t2.HandleCallCount);
        Assert.Equal(0, t3.HandleCallCount);
        Assert.Equal(0, oc.HandleCallCount);
    }

    [Fact]
    public void MediumPriority_PassesTier1_ReachesTier2()
    {
        var t1 = new PassThroughHandler();
        var t2 = new RecordingHandler();
        var t3 = new RecordingHandler();
        var oc = new RecordingHandler();
        t1.SetNext(t2).SetNext(t3).SetNext(oc);

        t1.Handle(new SupportTicket("T-2", "s", Priority.Medium, Category.Billing));

        Assert.Equal(1, t1.HandleCallCount);
        Assert.Equal(1, t2.HandleCallCount);
        Assert.Equal(0, t3.HandleCallCount);
        Assert.Equal(0, oc.HandleCallCount);
    }

    [Fact]
    public void HighPriority_PassesTier1AndTier2_ReachesTier3()
    {
        var t1 = new PassThroughHandler();
        var t2 = new PassThroughHandler();
        var t3 = new RecordingHandler();
        var oc = new RecordingHandler();
        t1.SetNext(t2).SetNext(t3).SetNext(oc);

        t1.Handle(new SupportTicket("T-3", "s", Priority.High, Category.Technical));

        Assert.Equal(1, t1.HandleCallCount);
        Assert.Equal(1, t2.HandleCallCount);
        Assert.Equal(1, t3.HandleCallCount);
        Assert.Equal(0, oc.HandleCallCount);
    }

    [Fact]
    public void CriticalPriority_PassesAllTiers_ReachesOncall()
    {
        var t1 = new PassThroughHandler();
        var t2 = new PassThroughHandler();
        var t3 = new PassThroughHandler();
        var oc = new RecordingHandler();
        t1.SetNext(t2).SetNext(t3).SetNext(oc);

        t1.Handle(new SupportTicket("T-4", "s", Priority.Critical, Category.Outage));

        Assert.Equal(1, t1.HandleCallCount);
        Assert.Equal(1, t2.HandleCallCount);
        Assert.Equal(1, t3.HandleCallCount);
        Assert.Equal(1, oc.HandleCallCount);
    }

    [Fact]
    public void ConcreteChain_LowPriority_ResolvedAtTier1_NeverReachesOncall()
    {
        var oncall = new RecordingHandler();
        var chain  = new Tier1Handler();
        chain.SetNext(new Tier2Handler()).SetNext(new Tier3Handler()).SetNext(oncall);

        chain.Handle(new SupportTicket("T-5", "s", Priority.Low, Category.Account));

        Assert.Equal(0, oncall.HandleCallCount);
    }

    [Fact]
    public void ConcreteChain_CriticalPriority_ReachesOncall()
    {
        var oncall = new RecordingHandler();
        var chain  = new Tier1Handler();
        chain.SetNext(new Tier2Handler()).SetNext(new Tier3Handler()).SetNext(oncall);

        var ticket = new SupportTicket("T-6", "s", Priority.Critical, Category.Outage);
        chain.Handle(ticket);

        Assert.Equal(1, oncall.HandleCallCount);
        Assert.Same(ticket, oncall.LastHandled);
    }
}
