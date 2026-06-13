namespace ChainOfResponsibilityPattern;

public abstract class TicketHandlerBase : ITicketHandler
{
    private ITicketHandler? _next;

    // Returns next so callers can fluently chain: tier1.SetNext(tier2).SetNext(tier3)
    public ITicketHandler SetNext(ITicketHandler next)
    {
        _next = next;
        return next;
    }

    public abstract void Handle(SupportTicket ticket);

    protected void PassToNext(SupportTicket ticket)
    {
        if (_next is not null)
            _next.Handle(ticket);
        else
            Console.WriteLine($"  [UNRESOLVED] #{ticket.Id} '{ticket.Subject}' — no handler could process this ticket");
    }
}
