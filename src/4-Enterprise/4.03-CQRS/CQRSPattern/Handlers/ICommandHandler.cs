namespace CQRSPattern;

public interface ICommandHandler<TCommand>
{
    CommandResult Handle(TCommand command);
}
