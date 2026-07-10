namespace StatePattern;

public sealed class HasMoneyState : IVendingState
{
    public static readonly HasMoneyState Instance = new();
    private HasMoneyState() { }

    public string Name => "HasMoney";

    public void InsertMoney(VendingMachine machine, decimal amount)
    {
        if (amount <= 0)
        {
            Console.WriteLine("  Please insert a positive amount.");
            return;
        }
        machine.Balance += amount;
        Console.WriteLine($"  Added ${amount:F2}. Balance: ${machine.Balance:F2}");
    }

    public void SelectProduct(VendingMachine machine, string code)
    {
        if (!machine.Inventory.TryGetValue(code, out var product))
        {
            Console.WriteLine($"  Product '{code}' not found.");
            return;
        }
        if (product.Stock == 0)
        {
            Console.WriteLine($"  '{product.Name}' is out of stock. Please choose another product.");
            return;
        }
        if (machine.Balance < product.Price)
        {
            Console.WriteLine($"  Insufficient funds. '{product.Name}' costs ${product.Price:F2}. Balance: ${machine.Balance:F2}");
            return;
        }
        machine.SelectedProduct = code;
        Console.WriteLine($"  Selected: {product.Name} (${product.Price:F2})");
        machine.TransitionTo(DispensingState.Instance);
        machine.Dispense();
    }

    public void ReturnMoney(VendingMachine machine)
    {
        Console.WriteLine($"  Returning ${machine.Balance:F2}.");
        machine.Balance = 0;
        machine.TransitionTo(IdleState.Instance);
    }

    public void Dispense(VendingMachine machine)
        => Console.WriteLine("  Please select a product first.");
}
