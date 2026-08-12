namespace DependencyInjectionPattern;

public sealed record OrderSummary(
    string             OrderId,
    IReadOnlyList<CartItem> Items,
    decimal            Subtotal,
    decimal            HstAmount,
    decimal            Total,
    DateTime           PlacedAt);
