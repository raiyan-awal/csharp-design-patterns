namespace CircuitBreakerPattern.Services;

public interface IShippingRateService
{
    ShippingRate GetRate(string origin, string destination, decimal weightKg);
}
