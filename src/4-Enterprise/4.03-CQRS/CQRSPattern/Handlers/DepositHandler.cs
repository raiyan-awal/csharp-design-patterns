namespace CQRSPattern;

public sealed class DepositHandler : ICommandHandler<DepositCommand>
{
    private readonly WriteStore       _writeStore;
    private readonly ReadStore        _readStore;
    private readonly AccountProjector _projector;

    public DepositHandler(WriteStore writeStore, ReadStore readStore, AccountProjector projector)
    {
        _writeStore = writeStore;
        _readStore  = readStore;
        _projector  = projector;
    }

    public CommandResult Handle(DepositCommand command)
    {
        var account = _writeStore.Find(command.AccountId);
        if (account is null)  return CommandResult.Fail($"Account '{command.AccountId}' not found.");
        if (command.Amount <= 0) return CommandResult.Fail("Deposit amount must be positive.");

        account.Deposit(command.Amount, command.Description);
        _readStore.Save(_projector.Project(account));

        Console.WriteLine($"  [CMD]   Deposit      → {command.AccountId}  +${command.Amount:F2}  ({command.Description})");
        return CommandResult.Ok();
    }
}
