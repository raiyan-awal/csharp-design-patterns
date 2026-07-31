namespace CQRSPattern;

public sealed class WithdrawHandler : ICommandHandler<WithdrawCommand>
{
    private readonly WriteStore       _writeStore;
    private readonly ReadStore        _readStore;
    private readonly AccountProjector _projector;

    public WithdrawHandler(WriteStore writeStore, ReadStore readStore, AccountProjector projector)
    {
        _writeStore = writeStore;
        _readStore  = readStore;
        _projector  = projector;
    }

    public CommandResult Handle(WithdrawCommand command)
    {
        var account = _writeStore.Find(command.AccountId);
        if (account is null)     return CommandResult.Fail($"Account '{command.AccountId}' not found.");
        if (command.Amount <= 0) return CommandResult.Fail("Withdrawal amount must be positive.");
        if (command.Amount > account.Balance)
            return CommandResult.Fail($"Insufficient funds. Balance: ${account.Balance:F2}, Requested: ${command.Amount:F2}");

        account.Withdraw(command.Amount, command.Description);
        _readStore.Save(_projector.Project(account));

        Console.WriteLine($"  [CMD]   Withdraw     → {command.AccountId}  -${command.Amount:F2}  ({command.Description})");
        return CommandResult.Ok();
    }
}
