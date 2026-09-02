namespace InboxPattern.Handlers;

public interface IMessageHandler<in TMessage>
{
    void Handle(TMessage message);
}
