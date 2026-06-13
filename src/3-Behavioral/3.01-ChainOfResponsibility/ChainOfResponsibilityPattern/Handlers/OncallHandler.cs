namespace ChainOfResponsibilityPattern;

public sealed class OncallHandler : TicketHandlerBase
{
    public override void Handle(SupportTicket ticket)
    {
        // On-call is the last link — handles everything that reaches here
        Console.WriteLine($"  [ON-CALL] PAGED for #{ticket.Id}: {ticket.Subject}  (priority={ticket.Priority}, category={ticket.Category})");
    }
}
