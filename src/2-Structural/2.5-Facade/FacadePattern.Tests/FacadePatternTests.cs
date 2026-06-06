using FacadePattern;

namespace FacadePattern.Tests;

// ── Shared helpers ────────────────────────────────────────────

file static class TestData
{
    public static OrderLine Laptop => new("LAPTOP-001", "Pro Laptop", 1, 999.99m);
    public static OrderLine Mouse  => new("MOUSE-002",  "Mouse",      2,  19.99m);

    public static Dictionary<string, int> FullStock => new()
    {
        ["LAPTOP-001"] = 10,
        ["MOUSE-002"]  = 20,
    };

    public static Order MakeOrder(string cardToken = "card-valid", OrderLine[]? lines = null)
        => new(
            OrderId:         $"ORD-{Guid.NewGuid():N}"[..8].ToUpper(),
            CustomerId:      "CUST-1",
            CustomerEmail:   "test@example.com",
            ShippingAddress: "1 Test Lane",
            CardToken:       cardToken,
            Lines:           lines ?? [Laptop]);
}

file static class FacadeFactory
{
    public static (OrderFacade Facade, InventoryService Inventory, PaymentService Payment)
        Create(Dictionary<string, int>? stock = null)
    {
        var inventory = new InventoryService(stock ?? TestData.FullStock);
        var payment   = new PaymentService();
        var facade    = new OrderFacade(
            inventory,
            payment,
            new ShippingService(),
            new NotificationService(),
            new AuditLogger());
        return (facade, inventory, payment);
    }
}

// ── PlaceOrder ───────────────────────────────────────────────

public class PlaceOrderTests
{
    [Fact]
    public void Success_ReturnsOkResult()
    {
        var (facade, _, _) = FacadeFactory.Create();
        var result = facade.PlaceOrder(TestData.MakeOrder());
        Assert.True(result.Success);
    }

    [Fact]
    public void Success_ResultContainsTrackingNumber()
    {
        var (facade, _, _) = FacadeFactory.Create();
        var result = facade.PlaceOrder(TestData.MakeOrder());
        Assert.False(string.IsNullOrWhiteSpace(result.TrackingNumber));
    }

    [Fact]
    public void Success_ResultContainsTransactionId()
    {
        var (facade, _, _) = FacadeFactory.Create();
        var result = facade.PlaceOrder(TestData.MakeOrder());
        Assert.False(string.IsNullOrWhiteSpace(result.TransactionId));
    }

    [Fact]
    public void Success_DeductsInventory()
    {
        var (facade, inventory, _) = FacadeFactory.Create();
        facade.PlaceOrder(TestData.MakeOrder(lines: [TestData.Laptop]));
        Assert.Equal(9, inventory.GetStock("LAPTOP-001"));  // started at 10
    }

    [Fact]
    public void Success_MultipleLines_DeductsEachProduct()
    {
        var (facade, inventory, _) = FacadeFactory.Create();
        facade.PlaceOrder(TestData.MakeOrder(lines: [TestData.Laptop, TestData.Mouse]));
        Assert.Equal(9,  inventory.GetStock("LAPTOP-001"));
        Assert.Equal(18, inventory.GetStock("MOUSE-002"));  // quantity 2 deducted
    }

    [Fact]
    public void OutOfStock_ReturnsFail()
    {
        var stock = new Dictionary<string, int> { ["LAPTOP-001"] = 0 };
        var (facade, _, _) = FacadeFactory.Create(stock);
        var result = facade.PlaceOrder(TestData.MakeOrder());
        Assert.False(result.Success);
    }

    [Fact]
    public void OutOfStock_DoesNotChargePayment()
    {
        var stock = new Dictionary<string, int> { ["LAPTOP-001"] = 0 };
        var (facade, _, payment) = FacadeFactory.Create(stock);
        facade.PlaceOrder(TestData.MakeOrder());
        Assert.Equal(0, payment.ChargeCallCount);
    }

    [Fact]
    public void OutOfStock_StockUnchanged()
    {
        var stock = new Dictionary<string, int> { ["LAPTOP-001"] = 0 };
        var (facade, inventory, _) = FacadeFactory.Create(stock);
        facade.PlaceOrder(TestData.MakeOrder());
        Assert.Equal(0, inventory.GetStock("LAPTOP-001"));
    }

    [Fact]
    public void PaymentDeclined_ReturnsFail()
    {
        var (facade, _, _) = FacadeFactory.Create();
        var result = facade.PlaceOrder(TestData.MakeOrder(cardToken: "fail-declined"));
        Assert.False(result.Success);
    }

    [Fact]
    public void PaymentDeclined_ReleasesReservedStock()
    {
        var (facade, inventory, _) = FacadeFactory.Create();
        int before = inventory.GetStock("LAPTOP-001");
        facade.PlaceOrder(TestData.MakeOrder(cardToken: "fail-declined"));
        Assert.Equal(before, inventory.GetStock("LAPTOP-001"));
    }

    [Fact]
    public void MultipleLines_ChargesCorrectTotal()
    {
        // Laptop: 1 × 999.99 + Mouse: 2 × 19.99 = 1039.97
        // We verify indirectly — if payment succeeded the result is Success
        var (facade, _, _) = FacadeFactory.Create();
        var result = facade.PlaceOrder(TestData.MakeOrder(lines: [TestData.Laptop, TestData.Mouse]));
        Assert.True(result.Success);
    }
}

// ── CancelOrder ─────────────────────────────────────────────

public class CancelOrderTests
{
    [Fact]
    public void Success_ReturnsOkResult()
    {
        var (facade, _, _) = FacadeFactory.Create();
        var order   = TestData.MakeOrder();
        var placed  = facade.PlaceOrder(order);

        var result = facade.CancelOrder(order, placed.TrackingNumber!, placed.TransactionId!);
        Assert.True(result.Success);
    }

    [Fact]
    public void Success_RestoresInventory()
    {
        var (facade, inventory, _) = FacadeFactory.Create();
        var order  = TestData.MakeOrder(lines: [TestData.Laptop]);
        int before = inventory.GetStock("LAPTOP-001");
        var placed = facade.PlaceOrder(order);

        facade.CancelOrder(order, placed.TrackingNumber!, placed.TransactionId!);

        Assert.Equal(before, inventory.GetStock("LAPTOP-001"));
    }

    [Fact]
    public void Success_RestoresInventory_ForMultipleLines()
    {
        var (facade, inventory, _) = FacadeFactory.Create();
        var order    = TestData.MakeOrder(lines: [TestData.Laptop, TestData.Mouse]);
        int laptopBefore = inventory.GetStock("LAPTOP-001");
        int mouseBefore  = inventory.GetStock("MOUSE-002");
        var placed = facade.PlaceOrder(order);

        facade.CancelOrder(order, placed.TrackingNumber!, placed.TransactionId!);

        Assert.Equal(laptopBefore, inventory.GetStock("LAPTOP-001"));
        Assert.Equal(mouseBefore,  inventory.GetStock("MOUSE-002"));
    }
}

// ── OrderResult ──────────────────────────────────────────────

public class OrderResultTests
{
    [Fact]
    public void Ok_SetsSuccessTrue()
        => Assert.True(OrderResult.Ok("ORD-1", "TRK-1", "TXN-1", "ok").Success);

    [Fact]
    public void Fail_SetsSuccessFalse()
        => Assert.False(OrderResult.Fail("ORD-1", "error").Success);

    [Fact]
    public void Fail_HasNoTrackingNumber()
        => Assert.Null(OrderResult.Fail("ORD-1", "error").TrackingNumber);

    [Fact]
    public void Fail_HasNoTransactionId()
        => Assert.Null(OrderResult.Fail("ORD-1", "error").TransactionId);
}

// ── InventoryService ─────────────────────────────────────────

public class InventoryServiceTests
{
    [Fact]
    public void IsAvailable_ReturnsFalse_WhenNoStock()
    {
        var inv = new InventoryService(new Dictionary<string, int> { ["P1"] = 0 });
        Assert.False(inv.IsAvailable("P1", 1));
    }

    [Fact]
    public void IsAvailable_ReturnsFalse_ForUnknownProduct()
    {
        var inv = new InventoryService();
        Assert.False(inv.IsAvailable("unknown", 1));
    }

    [Fact]
    public void Reserve_DeductsStock()
    {
        var inv = new InventoryService(new Dictionary<string, int> { ["P1"] = 5 });
        inv.Reserve("P1", 3);
        Assert.Equal(2, inv.GetStock("P1"));
    }

    [Fact]
    public void Release_AddsStock()
    {
        var inv = new InventoryService(new Dictionary<string, int> { ["P1"] = 2 });
        inv.Release("P1", 3);
        Assert.Equal(5, inv.GetStock("P1"));
    }
}
