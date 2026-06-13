namespace ChainOfResponsibilityPattern;

public sealed class Tier2Handler : TicketHandlerBase
{
    public override void Handle(SupportTicket ticket)
    {
        if (ticket.Priority == Priority.Medium)
        {
            Console.WriteLine($"  [TIER-2] Resolved #{ticket.Id}: {ticket.Subject}");
            return;
        }

        Console.WriteLine($"  [TIER-2] Cannot handle priority={ticket.Priority} — escalating to Tier-3");
        PassToNext(ticket);
    }
}
