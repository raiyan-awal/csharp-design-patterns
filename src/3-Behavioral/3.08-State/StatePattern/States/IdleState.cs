namespace StatePattern;

public sealed class IdleState : IVendingState
{
    public static readonly IdleState Instance = new();
    private IdleState() { }

    public string Name => "Idle";

    public void InsertMoney(VendingMachine machine, decimal amount)
    {
        if (amount <= 0)
        {
            Console.WriteLine("  Please insert a positive amount.");
            return;
        }
        machine.Balance += amount;
        Console.WriteLine($"  Inserted ${amount:F2}. Balance: ${machine.Balance:F2}");
        machine.TransitionTo(HasMoneyState.Instance);
    }

    public void SelectProduct(VendingMachine machine, string code)
        => Console.WriteLine("  Please insert money before selecting a product.");

    public void ReturnMoney(VendingMachine machine)
        => Console.WriteLine("  No money to return.");

    public void Dispense(VendingMachine machine)
        => Console.WriteLine("  Please insert money and select a product first.");
}
