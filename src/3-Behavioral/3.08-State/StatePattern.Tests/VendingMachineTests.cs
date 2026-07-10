using StatePattern;

namespace StatePattern.Tests;

public class VendingMachineTests
{
    private static VendingMachine BuildMachine()
    {
        var m = new VendingMachine();
        m.AddProduct("A1", "Water",     1.00m, 3);
        m.AddProduct("A2", "Cola",      1.50m, 2);
        m.AddProduct("B1", "Chips",     1.20m, 2);
        m.AddProduct("B2", "Chocolate", 2.00m, 1);
        return m;
    }

    // ── Initial state ───────────────────────────────────────────────────────

    [Fact]
    public void NewMachine_StartsInIdleState()
    {
        var machine = BuildMachine();
        Assert.Equal("Idle", machine.StateName);
    }

    [Fact]
    public void NewMachine_HasZeroBalance()
    {
        var machine = BuildMachine();
        Assert.Equal(0m, machine.Balance);
    }

    // ── InsertMoney ─────────────────────────────────────────────────────────

    [Fact]
    public void InsertMoney_FromIdle_TransitionsToHasMoney()
    {
        var machine = BuildMachine();
        machine.InsertMoney(1.00m);
        Assert.Equal("HasMoney", machine.StateName);
        Assert.Equal(1.00m, machine.Balance);
    }

    [Fact]
    public void InsertMoney_WhileInHasMoney_AccumulatesBalance()
    {
        var machine = BuildMachine();
        machine.InsertMoney(0.50m);
        machine.InsertMoney(0.50m);
        Assert.Equal(1.00m, machine.Balance);
        Assert.Equal("HasMoney", machine.StateName);
    }

    [Fact]
    public void InsertMoney_ZeroAmount_DoesNotTransitionFromIdle()
    {
        var machine = BuildMachine();
        machine.InsertMoney(0m);
        Assert.Equal("Idle", machine.StateName);
        Assert.Equal(0m, machine.Balance);
    }

    [Fact]
    public void InsertMoney_WhenOutOfStock_IsRejected()
    {
        var machine = new VendingMachine();
        machine.AddProduct("A1", "Water", 1.00m, 1);
        machine.InsertMoney(1.00m);
        machine.SelectProduct("A1"); // depletes stock → OutOfStock

        machine.InsertMoney(1.00m);
        Assert.Equal("OutOfStock", machine.StateName);
        Assert.Equal(0m, machine.Balance);
    }

    // ── SelectProduct ────────────────────────────────────────────────────────

    [Fact]
    public void SelectProduct_WithExactFunds_DispensesAndReturnsToIdle()
    {
        var machine = BuildMachine();
        machine.InsertMoney(1.00m);
        machine.SelectProduct("A1"); // Water £1.00

        Assert.Equal("Idle", machine.StateName);
        Assert.Equal(0m, machine.Balance);
        Assert.Equal(2, machine.Inventory["A1"].Stock);
    }

    [Fact]
    public void SelectProduct_WithOverpayment_ReturnsChange()
    {
        var machine = BuildMachine();
        machine.InsertMoney(2.00m);
        machine.SelectProduct("A2"); // Cola £1.50 — overpay by £0.50

        Assert.Equal("Idle", machine.StateName);
        Assert.Equal(0m, machine.Balance); // change returned → balance zero
    }

    [Fact]
    public void SelectProduct_WithInsufficientFunds_StaysInHasMoney()
    {
        var machine = BuildMachine();
        machine.InsertMoney(0.50m);
        machine.SelectProduct("A2"); // Cola £1.50 — not enough

        Assert.Equal("HasMoney", machine.StateName);
        Assert.Equal(0.50m, machine.Balance);
    }

    [Fact]
    public void SelectProduct_NonExistentCode_StaysInHasMoney()
    {
        var machine = BuildMachine();
        machine.InsertMoney(2.00m);
        machine.SelectProduct("Z9");

        Assert.Equal("HasMoney", machine.StateName);
        Assert.Equal(2.00m, machine.Balance);
    }

    [Fact]
    public void SelectProduct_OutOfStockItem_StaysInHasMoney()
    {
        var machine = BuildMachine();
        machine.UpdateStock("A1", 0); // manually zero out Water
        machine.InsertMoney(2.00m);
        machine.SelectProduct("A1");

        Assert.Equal("HasMoney", machine.StateName);
        Assert.Equal(2.00m, machine.Balance);
    }

    [Fact]
    public void SelectProduct_FromIdle_IsRejected()
    {
        var machine = BuildMachine();
        machine.SelectProduct("A1");
        Assert.Equal("Idle", machine.StateName);
    }

    // ── ReturnMoney ──────────────────────────────────────────────────────────

    [Fact]
    public void ReturnMoney_FromHasMoney_ResetsBalanceAndReturnsToIdle()
    {
        var machine = BuildMachine();
        machine.InsertMoney(1.50m);
        machine.ReturnMoney();

        Assert.Equal("Idle", machine.StateName);
        Assert.Equal(0m, machine.Balance);
    }

    [Fact]
    public void ReturnMoney_FromIdle_NoEffect()
    {
        var machine = BuildMachine();
        machine.ReturnMoney();
        Assert.Equal("Idle", machine.StateName);
        Assert.Equal(0m, machine.Balance);
    }

    // ── Out-of-stock transition ──────────────────────────────────────────────

    [Fact]
    public void DispensingLastItem_TransitionsToOutOfStock()
    {
        var machine = new VendingMachine();
        machine.AddProduct("A1", "Water", 1.00m, 1);
        machine.InsertMoney(1.00m);
        machine.SelectProduct("A1");

        Assert.Equal("OutOfStock", machine.StateName);
    }

    [Fact]
    public void Restock_FromOutOfStock_TransitionsToIdle()
    {
        var machine = new VendingMachine();
        machine.AddProduct("A1", "Water", 1.00m, 1);
        machine.InsertMoney(1.00m);
        machine.SelectProduct("A1"); // → OutOfStock

        machine.RestockProduct("A1", 5);
        Assert.Equal("Idle", machine.StateName);
        Assert.Equal(5, machine.Inventory["A1"].Stock);
    }

    [Fact]
    public void DispensingOneOfSeveralProducts_DoesNotTransitionToOutOfStock()
    {
        var machine = BuildMachine(); // multiple products with stock
        machine.InsertMoney(1.00m);
        machine.SelectProduct("A1");  // Water — only A1 stock drops

        Assert.Equal("Idle", machine.StateName); // other products still in stock
    }

    // ── Invalid operations ────────────────────────────────────────────────────

    [Fact]
    public void DispenseDirectly_FromIdle_NoStateChange()
    {
        var machine = BuildMachine();
        machine.Dispense();
        Assert.Equal("Idle", machine.StateName);
    }

    [Fact]
    public void InsertThenTopUpAndPurchase_WorksCorrectly()
    {
        var machine = BuildMachine();
        machine.InsertMoney(0.50m);
        machine.InsertMoney(0.50m);
        machine.InsertMoney(0.50m); // total £1.50
        machine.SelectProduct("A2"); // Cola £1.50

        Assert.Equal("Idle", machine.StateName);
        Assert.Equal(0m, machine.Balance);
        Assert.Equal(1, machine.Inventory["A2"].Stock);
    }
}
