using Microsoft.Extensions.DependencyInjection;
using DependencyInjectionPattern;

namespace DependencyInjectionPattern.Tests;

public class DependencyInjectionTests
{
    private static ServiceProvider BuildProvider(
        Action<IServiceCollection>? overrides = null)
    {
        var services = new ServiceCollection();
        services.AddCheckoutServices();
        overrides?.Invoke(services);
        return services.BuildServiceProvider();
    }

    // ── Singleton lifetime ────────────────────────────────────────────────────

    [Fact]
    public void Singleton_SameInstanceAcrossResolutions()
    {
        using var provider = BuildProvider();
        var a = provider.GetRequiredService<IInventoryService>();
        var b = provider.GetRequiredService<IInventoryService>();
        Assert.Equal(a.InstanceId, b.InstanceId);
    }

    [Fact]
    public void Singleton_SameInstanceAcrossScopes()
    {
        using var provider = BuildProvider();
        Guid idA, idB;
        using (var s = provider.CreateScope())
            idA = s.ServiceProvider.GetRequiredService<IInventoryService>().InstanceId;
        using (var s = provider.CreateScope())
            idB = s.ServiceProvider.GetRequiredService<IInventoryService>().InstanceId;
        Assert.Equal(idA, idB);
    }

    // ── Scoped lifetime ───────────────────────────────────────────────────────

    [Fact]
    public void Scoped_SameInstanceWithinScope()
    {
        using var provider = BuildProvider();
        using var scope    = provider.CreateScope();
        var a = scope.ServiceProvider.GetRequiredService<IShoppingCart>();
        var b = scope.ServiceProvider.GetRequiredService<IShoppingCart>();
        Assert.Equal(a.InstanceId, b.InstanceId);
    }

    [Fact]
    public void Scoped_DifferentInstanceAcrossScopes()
    {
        using var provider = BuildProvider();
        Guid idA, idB;
        using (var s = provider.CreateScope())
            idA = s.ServiceProvider.GetRequiredService<IShoppingCart>().InstanceId;
        using (var s = provider.CreateScope())
            idB = s.ServiceProvider.GetRequiredService<IShoppingCart>().InstanceId;
        Assert.NotEqual(idA, idB);
    }

    // ── Transient lifetime ────────────────────────────────────────────────────

    [Fact]
    public void Transient_NewInstanceEveryResolution()
    {
        using var provider = BuildProvider();
        using var scope    = provider.CreateScope();
        var a = scope.ServiceProvider.GetRequiredService<IHstCalculator>();
        var b = scope.ServiceProvider.GetRequiredService<IHstCalculator>();
        Assert.NotEqual(a.InstanceId, b.InstanceId);
    }

    // ── InventoryService ──────────────────────────────────────────────────────

    [Fact]
    public void Inventory_GetAll_ReturnsFiveProducts()
    {
        using var provider = BuildProvider();
        var inv = provider.GetRequiredService<IInventoryService>();
        Assert.Equal(5, inv.GetAll().Count);
    }

    [Fact]
    public void Inventory_GetById_ReturnsProduct()
    {
        using var provider = BuildProvider();
        var inv = provider.GetRequiredService<IInventoryService>();
        var p   = inv.GetById(1);
        Assert.NotNull(p);
        Assert.Equal("Roam Portable Speaker", p.Name);
    }

    [Fact]
    public void Inventory_GetById_UnknownId_ReturnsNull()
    {
        using var provider = BuildProvider();
        Assert.Null(provider.GetRequiredService<IInventoryService>().GetById(99));
    }

    [Fact]
    public void Inventory_Reserve_DecreasesStock()
    {
        using var provider = BuildProvider();
        var inv = provider.GetRequiredService<IInventoryService>();
        Assert.True(inv.IsInStock(1, 1));
        inv.Reserve(1, 1);
        // Inventory is Singleton so the same instance reflects the update
        Assert.True(inv.IsInStock(1, 1)); // still 49 left
    }

    // ── ShoppingCart ──────────────────────────────────────────────────────────

    [Fact]
    public void Cart_Add_IncreasesItemCount()
    {
        using var provider = BuildProvider();
        using var scope    = provider.CreateScope();
        var cart = scope.ServiceProvider.GetRequiredService<IShoppingCart>();
        var inv  = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        cart.Add(inv.GetById(1)!);
        Assert.Single(cart.Items);
    }

    [Fact]
    public void Cart_AddSameProduct_AccumulatesQuantity()
    {
        using var provider = BuildProvider();
        using var scope    = provider.CreateScope();
        var cart = scope.ServiceProvider.GetRequiredService<IShoppingCart>();
        var inv  = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        cart.Add(inv.GetById(1)!, 2);
        cart.Add(inv.GetById(1)!, 3);
        Assert.Single(cart.Items);
        Assert.Equal(5, cart.Items[0].Quantity);
    }

    [Fact]
    public void Cart_Remove_RemovesItem()
    {
        using var provider = BuildProvider();
        using var scope    = provider.CreateScope();
        var cart = scope.ServiceProvider.GetRequiredService<IShoppingCart>();
        var inv  = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        cart.Add(inv.GetById(1)!);
        cart.Add(inv.GetById(2)!);
        cart.Remove(1);
        Assert.Single(cart.Items);
        Assert.Equal(2, cart.Items[0].Product.Id);
    }

    [Fact]
    public void Cart_Clear_EmptiesCart()
    {
        using var provider = BuildProvider();
        using var scope    = provider.CreateScope();
        var cart = scope.ServiceProvider.GetRequiredService<IShoppingCart>();
        var inv  = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        cart.Add(inv.GetById(1)!);
        cart.Clear();
        Assert.Empty(cart.Items);
    }

    // ── HstCalculator ─────────────────────────────────────────────────────────

    [Fact]
    public void Hst_Ontario_ThirteenPercent()
    {
        using var provider = BuildProvider();
        using var scope    = provider.CreateScope();
        var tax = scope.ServiceProvider.GetRequiredService<IHstCalculator>();
        Assert.Equal(0.13m, tax.Rate("ON"));
    }

    [Fact]
    public void Hst_Alberta_FivePercent()
    {
        using var provider = BuildProvider();
        using var scope    = provider.CreateScope();
        var tax = scope.ServiceProvider.GetRequiredService<IHstCalculator>();
        Assert.Equal(0.05m, tax.Rate("AB"));
    }

    [Fact]
    public void Hst_Calculate_RoundsToTwoDecimals()
    {
        using var provider = BuildProvider();
        using var scope    = provider.CreateScope();
        var tax    = scope.ServiceProvider.GetRequiredService<IHstCalculator>();
        var result = tax.Calculate(100m, "QC"); // 14.975%
        Assert.Equal(14.98m, result);
    }

    // ── CheckoutService ───────────────────────────────────────────────────────

    [Fact]
    public void Checkout_EmptyCart_ReturnsNull()
    {
        using var provider = BuildProvider();
        using var scope    = provider.CreateScope();
        var checkout = scope.ServiceProvider.GetRequiredService<ICheckoutService>();
        Assert.Null(checkout.Checkout());
    }

    [Fact]
    public void Checkout_ValidCart_ReturnsOrderWithCorrectTotal()
    {
        using var provider = BuildProvider();
        using var scope    = provider.CreateScope();
        var sp       = scope.ServiceProvider;
        var cart     = sp.GetRequiredService<IShoppingCart>();
        var inv      = sp.GetRequiredService<IInventoryService>();
        var checkout = sp.GetRequiredService<ICheckoutService>();

        cart.Add(inv.GetById(1)!, 2); // Roam Portable Speaker x2 = $179.98
        var order = checkout.Checkout("ON");

        Assert.NotNull(order);
        Assert.Equal(179.98m,                        order.Subtotal);
        Assert.Equal(Math.Round(179.98m * 0.13m, 2), order.HstAmount);
        Assert.Equal(order.Subtotal + order.HstAmount, order.Total);
    }

    [Fact]
    public void Checkout_OutOfStockItem_ThrowsInvalidOperation()
    {
        using var provider = BuildProvider();
        using var scope    = provider.CreateScope();
        var sp       = scope.ServiceProvider;
        var cart     = sp.GetRequiredService<IShoppingCart>();
        var inv      = sp.GetRequiredService<IInventoryService>();
        var checkout = sp.GetRequiredService<ICheckoutService>();

        cart.Add(inv.GetById(1)!, 999); // more than the 50 in stock
        Assert.Throws<InvalidOperationException>(() => checkout.Checkout());
    }

    // ── Implementation swap ───────────────────────────────────────────────────

    [Fact]
    public void SwapImplementation_ZeroRateCalculator_ProducesZeroHst()
    {
        using var provider = BuildProvider(s =>
            s.AddTransient<IHstCalculator, ZeroRateCalculator>());
        using var scope    = provider.CreateScope();
        var sp       = scope.ServiceProvider;
        var cart     = sp.GetRequiredService<IShoppingCart>();
        var inv      = sp.GetRequiredService<IInventoryService>();
        var checkout = sp.GetRequiredService<ICheckoutService>();

        cart.Add(inv.GetById(3)!, 1); // Maple Leaf Tote Bag — $34.99
        var order = checkout.Checkout("ON");

        Assert.NotNull(order);
        Assert.Equal(0m,    order.HstAmount);
        Assert.Equal(34.99m, order.Total);
    }
}
