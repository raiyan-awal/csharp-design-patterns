namespace StatePattern;

public sealed class VendingMachine
{
    private IVendingState _state;
    private readonly Dictionary<string, Product> _inventory = new();

    public decimal Balance { get; internal set; }
    public string? SelectedProduct { get; internal set; }
    public IReadOnlyDictionary<string, Product> Inventory => _inventory;
    public string StateName => _state.Name;

    public VendingMachine()
    {
        _state = IdleState.Instance;
    }

    public void TransitionTo(IVendingState newState)
    {
        Console.WriteLine($"  [State] {_state.Name} → {newState.Name}");
        _state = newState;
    }

    public void AddProduct(string code, string name, decimal price, int stock)
        => _inventory[code] = new Product(code, name, price, stock);

    public void UpdateStock(string code, int newStock)
    {
        if (_inventory.TryGetValue(code, out var p))
            _inventory[code] = p with { Stock = newStock };
    }

    public void RestockProduct(string code, int additionalCount)
    {
        if (!_inventory.TryGetValue(code, out var p)) return;
        _inventory[code] = p with { Stock = p.Stock + additionalCount };
        Console.WriteLine($"  Restocked '{p.Name}' +{additionalCount} units. Total: {p.Stock + additionalCount}");
        if (_state is OutOfStockState && HasAnyStock())
            TransitionTo(IdleState.Instance);
    }

    public bool HasAnyStock() => _inventory.Values.Any(p => p.Stock > 0);

    // Context delegates all user operations to the current state
    public void InsertMoney(decimal amount) => _state.InsertMoney(this, amount);
    public void SelectProduct(string code)  => _state.SelectProduct(this, code);
    public void ReturnMoney()               => _state.ReturnMoney(this);
    public void Dispense()                  => _state.Dispense(this);
}
