namespace StatePattern;

public sealed class OutOfStockState : IVendingState
{
    public static readonly OutOfStockState Instance = new();
    private OutOfStockState() { }

    public string Name => "OutOfStock";

    public void InsertMoney(VendingMachine machine, decimal amount)
        => Console.WriteLine("  Machine is out of stock. Cannot accept money.");

    public void SelectProduct(VendingMachine machine, string code)
        => Console.WriteLine("  Machine is out of stock. Please try again later.");

    public void ReturnMoney(VendingMachine machine)
    {
        if (machine.Balance > 0)
        {
            Console.WriteLine($"  Returning ${machine.Balance:F2} (machine out of stock).");
            machine.Balance = 0;
        }
        else
        {
            Console.WriteLine("  No money to return.");
        }
    }

    public void Dispense(VendingMachine machine)
        => Console.WriteLine("  Machine is out of stock. Cannot dispense.");
}
