namespace ChainOfResponsibilityPattern;

public sealed class Tier1Handler : TicketHandlerBase
{
    public override void Handle(SupportTicket ticket)
    {
        if (ticket.Priority == Priority.Low)
        {
            Console.WriteLine($"  [TIER-1] Resolved #{ticket.Id}: {ticket.Subject}");
            return;
        }

        Console.WriteLine($"  [TIER-1] Cannot handle priority={ticket.Priority} — escalating to Tier-2");
        PassToNext(ticket);
    }
}
