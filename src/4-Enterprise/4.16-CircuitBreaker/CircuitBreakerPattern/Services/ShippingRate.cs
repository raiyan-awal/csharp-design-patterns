namespace CircuitBreakerPattern.Services;

public sealed record ShippingRate(
    string  Carrier,
    string  Service,
    decimal PriceCAD,
    int     EstimatedDays);
