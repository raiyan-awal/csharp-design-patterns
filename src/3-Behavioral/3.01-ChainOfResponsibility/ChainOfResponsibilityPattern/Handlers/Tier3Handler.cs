namespace ChainOfResponsibilityPattern;

public sealed class Tier3Handler : TicketHandlerBase
{
    public override void Handle(SupportTicket ticket)
    {
        if (ticket.Priority == Priority.High)
        {
            Console.WriteLine($"  [TIER-3] Resolved #{ticket.Id}: {ticket.Subject}");
            return;
        }

        Console.WriteLine($"  [TIER-3] Cannot handle priority={ticket.Priority}, category={ticket.Category} — escalating to On-Call");
        PassToNext(ticket);
    }
}
