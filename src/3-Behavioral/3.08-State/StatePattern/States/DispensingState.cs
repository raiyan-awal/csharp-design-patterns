namespace StatePattern;

public sealed class DispensingState : IVendingState
{
    public static readonly DispensingState Instance = new();
    private DispensingState() { }

    public string Name => "Dispensing";

    public void InsertMoney(VendingMachine machine, decimal amount)
        => Console.WriteLine("  Please wait — dispensing in progress.");

    public void SelectProduct(VendingMachine machine, string code)
        => Console.WriteLine("  Please wait — dispensing in progress.");

    public void ReturnMoney(VendingMachine machine)
        => Console.WriteLine("  Cannot return money while dispensing.");

    public void Dispense(VendingMachine machine)
    {
        var code = machine.SelectedProduct!;
        var product = machine.Inventory[code];
        var change = machine.Balance - product.Price;

        machine.UpdateStock(code, product.Stock - 1);
        machine.Balance = 0;
        machine.SelectedProduct = null;

        Console.WriteLine($"  Dispensing: {product.Name}");
        if (change > 0)
            Console.WriteLine($"  Change returned: ${change:F2}");

        if (!machine.HasAnyStock())
        {
            Console.WriteLine("  Machine is now out of stock.");
            machine.TransitionTo(OutOfStockState.Instance);
        }
        else
        {
            machine.TransitionTo(IdleState.Instance);
        }
    }
}
