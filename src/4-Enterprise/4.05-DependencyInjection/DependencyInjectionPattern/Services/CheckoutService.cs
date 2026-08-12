namespace DependencyInjectionPattern;

// Registered as Scoped — lives for the duration of one checkout session.
// Receives its dependencies through the constructor; never creates them itself.
public sealed class CheckoutService : ICheckoutService
{
    public Guid InstanceId { get; } = Guid.NewGuid();

    private readonly IShoppingCart    _cart;
    private readonly IInventoryService _inventory;
    private readonly IHstCalculator   _tax;

    public CheckoutService(IShoppingCart cart, IInventoryService inventory, IHstCalculator tax)
    {
        _cart      = cart;
        _inventory = inventory;
        _tax       = tax;
    }

    public OrderSummary? Checkout(string province = "ON")
    {
        if (_cart.Items.Count == 0)
            return null;

        foreach (var item in _cart.Items)
            if (!_inventory.IsInStock(item.Product.Id, item.Quantity))
                throw new InvalidOperationException(
                    $"'{item.Product.Name}' does not have {item.Quantity} unit(s) in stock.");

        foreach (var item in _cart.Items)
            _inventory.Reserve(item.Product.Id, item.Quantity);

        var subtotal = _cart.Subtotal;
        var hst      = _tax.Calculate(subtotal, province);

        return new OrderSummary(
            OrderId:   $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Items:     _cart.Items,
            Subtotal:  subtotal,
            HstAmount: hst,
            Total:     subtotal + hst,
            PlacedAt:  DateTime.UtcNow);
    }
}
