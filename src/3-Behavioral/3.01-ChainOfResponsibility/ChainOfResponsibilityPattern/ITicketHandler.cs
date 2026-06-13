namespace ChainOfResponsibilityPattern;

public interface ITicketHandler
{
    ITicketHandler SetNext(ITicketHandler next);
    void Handle(SupportTicket ticket);
}
