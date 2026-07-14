using VisitorPattern;

namespace VisitorPattern.Tests;

public class VisitorTests
{
    // ── TaxVisitor ───────────────────────────────────────────────────────────

    [Fact]
    public void Tax_PhysicalProduct_Applies13Percent()
    {
        var visitor = new TaxVisitor();
        new PhysicalProduct("Test", 100.00m, 1.0).Accept(visitor);
        Assert.Equal(13.00m, visitor.TotalTax);
    }

    [Fact]
    public void Tax_DigitalProduct_Applies13Percent()
    {
        var visitor = new TaxVisitor();
        new DigitalProduct("Test", 100.00m).Accept(visitor);
        Assert.Equal(13.00m, visitor.TotalTax);
    }

    [Fact]
    public void Tax_FoodItem_IsZero()
    {
        var visitor = new TaxVisitor();
        new FoodItem("Test", 100.00m).Accept(visitor);
        Assert.Equal(0m, visitor.TotalTax);
    }

    [Fact]
    public void Tax_SubscriptionService_AppliesOnTotalPrice()
    {
        var visitor = new TaxVisitor();
        new SubscriptionService("Test", 10.00m, months: 12).Accept(visitor); // total = $120
        Assert.Equal(15.60m, visitor.TotalTax);
    }

    [Fact]
    public void Tax_AccumulatesAcrossMultipleItems()
    {
        var visitor = new TaxVisitor();
        new PhysicalProduct("A", 100.00m, 1.0).Accept(visitor); // $13
        new DigitalProduct ("B", 200.00m).Accept(visitor);      // $26
        new FoodItem       ("C",  50.00m).Accept(visitor);      // $0
        Assert.Equal(39.00m, visitor.TotalTax);
    }

    // ── ShippingVisitor ──────────────────────────────────────────────────────

    [Fact]
    public void Shipping_PhysicalProduct_IsBaseRatePlusWeightRate()
    {
        var visitor = new ShippingVisitor();
        new PhysicalProduct("Test", 100m, weightKg: 2.0).Accept(visitor);
        // $4.99 + 2.0 × $1.50 = $7.99
        Assert.Equal(7.99m, visitor.TotalShipping);
    }

    [Fact]
    public void Shipping_DigitalProduct_IsZero()
    {
        var visitor = new ShippingVisitor();
        new DigitalProduct("Test", 100m).Accept(visitor);
        Assert.Equal(0m, visitor.TotalShipping);
    }

    [Fact]
    public void Shipping_FoodItem_NonRefrigerated_IsBaseRate()
    {
        var visitor = new ShippingVisitor();
        new FoodItem("Test", 10m, isRefrigerated: false).Accept(visitor);
        Assert.Equal(4.99m, visitor.TotalShipping);
    }

    [Fact]
    public void Shipping_FoodItem_Refrigerated_HasSurcharge()
    {
        var visitor = new ShippingVisitor();
        new FoodItem("Test", 10m, isRefrigerated: true).Accept(visitor);
        Assert.Equal(9.99m, visitor.TotalShipping); // $4.99 + $5.00
    }

    [Fact]
    public void Shipping_SubscriptionService_IsZero()
    {
        var visitor = new ShippingVisitor();
        new SubscriptionService("Test", 9.99m, months: 1).Accept(visitor);
        Assert.Equal(0m, visitor.TotalShipping);
    }

    [Fact]
    public void Shipping_AccumulatesAcrossMultipleItems()
    {
        var visitor = new ShippingVisitor();
        new PhysicalProduct("A", 100m, 1.0).Accept(visitor); // $4.99 + $1.50 = $6.49
        new FoodItem("B", 10m, isRefrigerated: true).Accept(visitor);  // $9.99
        new DigitalProduct("C", 50m).Accept(visitor);                  // $0
        Assert.Equal(16.48m, visitor.TotalShipping);
    }

    // ── ReceiptVisitor ───────────────────────────────────────────────────────

    [Fact]
    public void Receipt_GeneratesOneLinePerItem()
    {
        var visitor = new ReceiptVisitor();
        new PhysicalProduct("Laptop", 999m, 2.0).Accept(visitor);
        new DigitalProduct ("App",     49m).Accept(visitor);
        new FoodItem       ("Milk",     4m).Accept(visitor);
        Assert.Equal(3, visitor.Lines.Count);
    }

    [Fact]
    public void Receipt_FoodItemLine_ContainsTaxExemptLabel()
    {
        var visitor = new ReceiptVisitor();
        new FoodItem("Apples", 8.99m).Accept(visitor);
        Assert.Contains("Tax exempt", visitor.Lines[0]);
    }

    [Fact]
    public void Receipt_DigitalProductLine_ContainsDigitalLabel()
    {
        var visitor = new ReceiptVisitor();
        new DigitalProduct("App", 29.99m).Accept(visitor);
        Assert.Contains("Digital", visitor.Lines[0]);
    }

    [Fact]
    public void Receipt_SubscriptionLine_ContainsMonthlyBreakdown()
    {
        var visitor = new ReceiptVisitor();
        new SubscriptionService("Prime", 9.99m, months: 12).Accept(visitor);
        Assert.Contains("9.99/mo", visitor.Lines[0]);
    }

    // ── LoyaltyPointsVisitor ─────────────────────────────────────────────────

    [Fact]
    public void Points_PhysicalProduct_IsOnePerDollar()
    {
        var visitor = new LoyaltyPointsVisitor();
        new PhysicalProduct("Test", 150.00m, 1.0).Accept(visitor);
        Assert.Equal(150, visitor.TotalPoints);
    }

    [Fact]
    public void Points_DigitalProduct_IsTwoPerDollar()
    {
        var visitor = new LoyaltyPointsVisitor();
        new DigitalProduct("Test", 100.00m).Accept(visitor);
        Assert.Equal(200, visitor.TotalPoints);
    }

    [Fact]
    public void Points_FoodItem_IsZero()
    {
        var visitor = new LoyaltyPointsVisitor();
        new FoodItem("Test", 50.00m).Accept(visitor);
        Assert.Equal(0, visitor.TotalPoints);
    }

    [Fact]
    public void Points_SubscriptionService_Is50PerMonth()
    {
        var visitor = new LoyaltyPointsVisitor();
        new SubscriptionService("Test", 9.99m, months: 6).Accept(visitor);
        Assert.Equal(300, visitor.TotalPoints);
    }
}
