namespace StatePattern;

public interface IVendingState
{
    string Name { get; }
    void InsertMoney(VendingMachine machine, decimal amount);
    void SelectProduct(VendingMachine machine, string code);
    void ReturnMoney(VendingMachine machine);
    void Dispense(VendingMachine machine);
}
