namespace FacadePattern;

public sealed record Order(
    string OrderId,
    string CustomerId,
    string CustomerEmail,
    string ShippingAddress,
    string CardToken,
    IReadOnlyList<OrderLine> Lines
);
