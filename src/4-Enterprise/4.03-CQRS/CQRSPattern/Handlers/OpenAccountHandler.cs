namespace CQRSPattern;

public sealed class OpenAccountHandler : ICommandHandler<OpenAccountCommand>
{
    private readonly WriteStore       _writeStore;
    private readonly ReadStore        _readStore;
    private readonly AccountProjector _projector;

    public OpenAccountHandler(WriteStore writeStore, ReadStore readStore, AccountProjector projector)
    {
        _writeStore = writeStore;
        _readStore  = readStore;
        _projector  = projector;
    }

    public CommandResult Handle(OpenAccountCommand command)
    {
        if (_writeStore.Exists(command.AccountId))
            return CommandResult.Fail($"Account '{command.AccountId}' already exists.");
        if (command.InitialDeposit < 0)
            return CommandResult.Fail("Initial deposit cannot be negative.");

        var account = new BankAccount(command.AccountId, command.OwnerName, command.InitialDeposit);
        _writeStore.Save(account);
        _readStore.Save(_projector.Project(account));

        Console.WriteLine($"  [CMD]   OpenAccount  → {command.OwnerName} ({command.AccountId}), opening balance ${command.InitialDeposit:F2}");
        return CommandResult.Ok();
    }
}
